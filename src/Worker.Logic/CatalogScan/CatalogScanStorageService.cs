﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace NuGet.Insights.Worker
{
    public class CatalogScanStorageService
    {
        private static readonly IReadOnlyDictionary<string, CatalogLeafScan> EmptyLeafIdToLeafScans = new Dictionary<string, CatalogLeafScan>();

        private readonly ServiceClientFactory _serviceClientFactory;
        private readonly ITelemetryClient _telemetryClient;
        private readonly IOptions<NuGetInsightsWorkerSettings> _options;
        private readonly ILogger<CatalogScanStorageService> _logger;

        public CatalogScanStorageService(
            ServiceClientFactory serviceClientFactory,
            ITelemetryClient telemetryClient,
            IOptions<NuGetInsightsWorkerSettings> options,
            ILogger<CatalogScanStorageService> logger)
        {
            _serviceClientFactory = serviceClientFactory;
            _telemetryClient = telemetryClient;
            _options = options;
            _logger = logger;
        }

        public async Task InitializeAsync()
        {
            await (await GetIndexScanTableAsync()).CreateIfNotExistsAsync(retry: true);
        }

        public async Task InitializePageScanTableAsync(string storageSuffix)
        {
            await (await GetPageScanTableAsync(storageSuffix)).CreateIfNotExistsAsync(retry: true);
        }

        public async Task InitializeLeafScanTableAsync(string storageSuffix)
        {
            await (await GetLeafScanTableAsync(storageSuffix)).CreateIfNotExistsAsync(retry: true);
        }

        public async Task DeleteChildTablesAsync(string storageSuffix)
        {
            await (await GetLeafScanTableAsync(storageSuffix)).DeleteAsync();
            await (await GetPageScanTableAsync(storageSuffix)).DeleteAsync();
        }

        public async Task InsertAsync(CatalogIndexScan indexScan)
        {
            var table = await GetIndexScanTableAsync();
            await table.AddEntityAsync(indexScan);
        }

        public async Task<IReadOnlyList<CatalogPageScan>> GetPageScansAsync(string storageSuffix, string scanId)
        {
            var table = await GetPageScanTableAsync(storageSuffix);
            return await table
                .QueryAsync<CatalogPageScan>(x => x.PartitionKey == scanId)
                .ToListAsync(_telemetryClient.StartQueryLoopMetrics());
        }

        public async Task<IReadOnlyList<CatalogLeafScan>> GetLeafScansAsync(string storageSuffix, string scanId, string pageId)
        {
            var table = await GetLeafScanTableAsync(storageSuffix);
            return await table
                .QueryAsync<CatalogLeafScan>(x => x.PartitionKey == CatalogLeafScan.GetPartitionKey(scanId, pageId))
                .ToListAsync(_telemetryClient.StartQueryLoopMetrics());
        }

        public async Task<IReadOnlyDictionary<string, CatalogLeafScan>> GetLeafScansAsync(string storageSuffix, string scanId, string pageId, IEnumerable<string> leafIds)
        {
            var sortedLeafIds = leafIds.OrderBy(x => x).ToList();
            if (sortedLeafIds.Count == 0)
            {
                return EmptyLeafIdToLeafScans;
            }
            else if (sortedLeafIds.Count == 1)
            {
                var leafScan = await GetLeafScanAsync(storageSuffix, scanId, pageId, sortedLeafIds[0]);
                if (leafScan == null)
                {
                    return EmptyLeafIdToLeafScans;
                }
                else
                {
                    return new Dictionary<string, CatalogLeafScan> { { leafScan.GetLeafId(), leafScan } };
                }
            }

            var table = await GetLeafScanTableAsync(storageSuffix);
            var leafScans = await table
                .QueryAsync<CatalogLeafScan>(x => x.PartitionKey == CatalogLeafScan.GetPartitionKey(scanId, pageId))
                .ToListAsync(_telemetryClient.StartQueryLoopMetrics());
            var uniqueLeafIds = sortedLeafIds.ToHashSet();
            return leafScans
                .Where(x => uniqueLeafIds.Contains(x.GetLeafId()))
                .ToDictionary(x => x.GetLeafId());
        }

        public async Task InsertAsync(IReadOnlyList<CatalogPageScan> pageScans)
        {
            foreach (var group in pageScans.GroupBy(x => x.StorageSuffix))
            {
                var table = await GetPageScanTableAsync(group.Key);
                await SubmitBatchesAsync(group.Key, table, group, (b, i) => b.AddEntity(i));
            }
        }

        public async Task InsertAsync(IReadOnlyList<CatalogLeafScan> leafScans)
        {
            foreach (var group in leafScans.GroupBy(x => x.StorageSuffix))
            {
                var table = await GetLeafScanTableAsync(group.Key);
                await SubmitBatchesAsync(group.Key, table, group, (b, i) => b.AddEntity(i));
            }
        }

        private async Task SubmitBatchesAsync<T>(
            string storageSuffix,
            TableClient table,
            IEnumerable<T> entities,
            Action<MutableTableTransactionalBatch, T> doOperation) where T : class, ITableEntity, new()
        {
            T firstEntity = null;
            try
            {
                var batch = new MutableTableTransactionalBatch(table);
                foreach (var entity in entities)
                {
                    if (batch.Count >= StorageUtility.MaxBatchSize)
                    {
                        await batch.SubmitBatchAsync();
                        batch = new MutableTableTransactionalBatch(table);
                    }

                    doOperation(batch, entity);
                    if (batch.Count == 1)
                    {
                        firstEntity = entity;
                    }
                }

                await batch.SubmitBatchIfNotEmptyAsync();
            }
            catch (RequestFailedException ex) when (ex.Status > 0)
            {
                _logger.LogWarning(
                    ex,
                    "Batch failed to due to HTTP {Status}, with storage suffix '{StorageSuffix}', first partition key '{PartitionKey}', first row key '{RowKey}'.",
                    ex.Status,
                    storageSuffix,
                    firstEntity.PartitionKey,
                    firstEntity.RowKey);
                throw;
            }
        }

        public async Task<IReadOnlyList<CatalogIndexScan>> GetIndexScansAsync()
        {
            return await (await GetIndexScanTableAsync())
                .QueryAsync<CatalogIndexScan>()
                .ToListAsync(_telemetryClient.StartQueryLoopMetrics());
        }

        public async Task<IReadOnlyList<CatalogIndexScan>> GetLatestIndexScansAsync(string cursorName, int maxEntities)
        {
            return await (await GetIndexScanTableAsync())
                .QueryAsync<CatalogIndexScan>(x => x.PartitionKey == cursorName)
                .Take(maxEntities)
                .ToListAsync();
        }

        public async Task DeleteOldIndexScansAsync(string cursorName, string currentScanId)
        {
            var table = await GetIndexScanTableAsync();
            var oldScans = await table
                .QueryAsync<CatalogIndexScan>(x => x.PartitionKey == cursorName
                                                && x.RowKey.CompareTo(currentScanId) > 0)
                .ToListAsync(_telemetryClient.StartQueryLoopMetrics());

            var oldScansToDelete = oldScans
                .OrderByDescending(x => x.Created)
                .Skip(_options.Value.OldCatalogIndexScansToKeep)
                .OrderBy(x => x.Created)
                .Where(x => x.State == CatalogIndexScanState.Complete)
                .ToList();
            _logger.LogInformation("Deleting {Count} old catalog index scans.", oldScansToDelete.Count);

            var batch = new MutableTableTransactionalBatch(table);
            foreach (var scan in oldScansToDelete)
            {
                if (batch.Count >= StorageUtility.MaxBatchSize)
                {
                    await batch.SubmitBatchAsync();
                    batch = new MutableTableTransactionalBatch(table);
                }

                batch.DeleteEntity(scan.PartitionKey, scan.RowKey, scan.ETag);
            }

            await batch.SubmitBatchIfNotEmptyAsync();
        }

        public async Task<CatalogIndexScan> GetIndexScanAsync(string cursorName, string scanId)
        {
            return await (await GetIndexScanTableAsync())
                .GetEntityOrNullAsync<CatalogIndexScan>(cursorName, scanId);
        }

        public async Task<CatalogPageScan> GetPageScanAsync(string storageSuffix, string scanId, string pageId)
        {
            return await (await GetPageScanTableAsync(storageSuffix))
                .GetEntityOrNullAsync<CatalogPageScan>(scanId, pageId);
        }

        public async Task<CatalogLeafScan> GetLeafScanAsync(string storageSuffix, string scanId, string pageId, string leafId)
        {
            return await (await GetLeafScanTableAsync(storageSuffix))
                .GetEntityOrNullAsync<CatalogLeafScan>(CatalogLeafScan.GetPartitionKey(scanId, pageId), leafId);
        }

        public async Task ReplaceAsync(CatalogIndexScan indexScan)
        {
            _logger.LogInformation("Replacing catalog index scan {ScanId}, state: {State}.", indexScan.GetScanId(), indexScan.State);
            var response = await (await GetIndexScanTableAsync()).UpdateEntityAsync(indexScan, indexScan.ETag);
            indexScan.UpdateETag(response);
        }

        public async Task ReplaceAsync(CatalogPageScan pageScan)
        {
            _logger.LogInformation("Replacing catalog page scan {ScanId}, page {PageId}, state: {State}.", pageScan.GetScanId(), pageScan.GetPageId(), pageScan.State);
            var response = await (await GetPageScanTableAsync(pageScan.StorageSuffix)).UpdateEntityAsync(pageScan, pageScan.ETag);
            pageScan.UpdateETag(response);
        }

        public async Task ReplaceAsync(CatalogLeafScan leafScan)
        {
            var response = await (await GetLeafScanTableAsync(leafScan.StorageSuffix)).UpdateEntityAsync(leafScan, leafScan.ETag);
            leafScan.UpdateETag(response);
        }

        public async Task ReplaceAsync(IEnumerable<CatalogLeafScan> leafScans)
        {
            await SubmitLeafBatchesAsync(leafScans, (b, i) => b.UpdateEntity(i, i.ETag, TableUpdateMode.Replace));
        }

        public async Task DeleteAsync(IEnumerable<CatalogLeafScan> leafScans)
        {
            var leafScansList = leafScans.ToList();
            if (leafScansList.Count == 1)
            {
                await DeleteAsync(leafScansList[0]);
                return;
            }

            try
            {
                await SubmitLeafBatchesAsync(leafScansList, (b, i) => b.DeleteEntity(i.PartitionKey, i.RowKey, i.ETag));
            }
            catch (RequestFailedException ex) when (ex.Status == (int)HttpStatusCode.NotFound)
            {
                // Try individually, to ensure each entity is deleted if it exists.
                foreach (var scan in leafScansList)
                {
                    await DeleteAsync(scan);
                }
            }
        }

        private async Task SubmitLeafBatchesAsync(IEnumerable<CatalogLeafScan> leafScans, Action<MutableTableTransactionalBatch, CatalogLeafScan> doOperation)
        {
            if (!leafScans.Any())
            {
                return;
            }

            var storageSuffixAndPartitionKeys = leafScans.Select(x => new { x.StorageSuffix, x.PartitionKey }).Distinct();
            if (storageSuffixAndPartitionKeys.Count() > 1)
            {
                throw new ArgumentException("All leaf scans must have the same storage suffix and partition key.");
            }

            var storageSuffix = storageSuffixAndPartitionKeys.Single().StorageSuffix;
            var table = await GetLeafScanTableAsync(storageSuffix);
            await SubmitBatchesAsync(storageSuffix, table, leafScans, doOperation);
        }

        public async Task<int> GetPageScanCountLowerBoundAsync(string storageSuffix, string scanId)
        {
            var table = await GetPageScanTableAsync(storageSuffix);
            return await table.GetEntityCountLowerBoundAsync(scanId, _telemetryClient.StartQueryLoopMetrics());
        }

        public async Task<int> GetLeafScanCountLowerBoundAsync(string storageSuffix, string scanId)
        {
            var table = await GetLeafScanTableAsync(storageSuffix);
            return await table.GetEntityCountLowerBoundAsync(
                CatalogLeafScan.GetPartitionKey(scanId, string.Empty),
                CatalogLeafScan.GetPartitionKey(scanId, char.MaxValue.ToString()),
                _telemetryClient.StartQueryLoopMetrics());
        }

        public async Task DeleteAsync(CatalogIndexScan indexScan)
        {
            await (await GetIndexScanTableAsync()).DeleteEntityAsync(indexScan, indexScan.ETag);
        }

        public async Task DeleteAsync(CatalogPageScan pageScan)
        {
            await (await GetPageScanTableAsync(pageScan.StorageSuffix)).DeleteEntityAsync(pageScan, pageScan.ETag);
        }

        public async Task DeleteAsync(CatalogLeafScan leafScan)
        {
            try
            {
                await (await GetLeafScanTableAsync(leafScan.StorageSuffix)).DeleteEntityAsync(leafScan, leafScan.ETag);
            }
            catch (RequestFailedException ex) when (ex.Status == (int)HttpStatusCode.NotFound)
            {
                _logger.LogWarning(
                    ex,
                    "Catalog leaf scan with storage suffix {StorageSuffix}, partition key {PartitionKey}, and row key {RowKey} was already deleted.",
                    leafScan.StorageSuffix,
                    leafScan.PartitionKey,
                    leafScan.RowKey);
            }
        }

        private async Task<TableClient> GetIndexScanTableAsync()
        {
            return (await _serviceClientFactory.GetTableServiceClientAsync())
                .GetTableClient(_options.Value.CatalogIndexScanTableName);
        }

        private async Task<TableClient> GetPageScanTableAsync(string suffix)
        {
            return (await _serviceClientFactory.GetTableServiceClientAsync())
                .GetTableClient($"{_options.Value.CatalogPageScanTableName}{suffix}");
        }

        public string GetLeafScanTableName(string suffix)
        {
            return $"{_options.Value.CatalogLeafScanTableName}{suffix}";
        }

        public async Task<TableClient> GetLeafScanTableAsync(string suffix)
        {
            return (await _serviceClientFactory.GetTableServiceClientAsync())
                .GetTableClient(GetLeafScanTableName(suffix));
        }
    }
}
