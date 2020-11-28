﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Knapcode.ExplorePackages.Worker.FindPackageAssets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Knapcode.ExplorePackages.Worker
{
    public class CatalogScanService
    {
        private readonly CatalogClient _catalogClient;
        private readonly CursorStorageService _cursorStorageService;
        private readonly MessageEnqueuer _messageEnqueuer;
        private readonly SchemaSerializer _serializer;
        private readonly CatalogScanStorageService _catalogScanStorageService;
        private readonly IOptionsSnapshot<ExplorePackagesWorkerSettings> _options;
        private readonly ILogger<CatalogScanService> _logger;

        public CatalogScanService(
            CatalogClient catalogClient,
            CursorStorageService cursorStorageService,
            MessageEnqueuer messageEnqueuer,
            SchemaSerializer serializer,
            CatalogScanStorageService catalogScanStorageService,
            IOptionsSnapshot<ExplorePackagesWorkerSettings> options,
            ILogger<CatalogScanService> logger)
        {
            _catalogClient = catalogClient;
            _cursorStorageService = cursorStorageService;
            _messageEnqueuer = messageEnqueuer;
            _serializer = serializer;
            _catalogScanStorageService = catalogScanStorageService;
            _options = options;
            _logger = logger;
        }

        public async Task RequeueAsync(string scanId)
        {
            var indexScan = await _catalogScanStorageService.GetIndexScanAsync(scanId);
            if (indexScan.ParsedState != CatalogScanState.Waiting)
            {
                return;
            }

            var pageScans = await _catalogScanStorageService.GetPageScansAsync(indexScan.StorageSuffix, indexScan.ScanId);
            foreach (var pageScan in pageScans)
            {
                var leafScans = await _catalogScanStorageService.GetLeafScansAsync(pageScan.StorageSuffix, pageScan.ScanId, pageScan.PageId);
                await _messageEnqueuer.EnqueueAsync(leafScans
                    .Select(x => new CatalogLeafScanMessage
                    {
                        StorageSuffix = x.StorageSuffix,
                        ScanId = x.ScanId,
                        PageId = x.PageId,
                        LeafId = x.LeafId,
                    })
                    .ToList());
            }

            await _messageEnqueuer.EnqueueAsync(pageScans
                .Select(x => new CatalogPageScanMessage
                {
                    StorageSuffix = x.StorageSuffix,
                    ScanId = x.ScanId,
                    PageId = x.PageId,
                })
                .ToList());

            await _messageEnqueuer.EnqueueAsync(new[]
            {
                new CatalogIndexScanMessage
                {
                    ScanId = indexScan.ScanId,
                },
            });
        }

        public async Task<CatalogIndexScan> UpdateFindPackageAssets() => await UpdateFindPackageAssets(max: null);
        public async Task<CatalogIndexScan> UpdateFindPackageAssetsAsync(DateTimeOffset max) => await UpdateFindPackageAssets((DateTimeOffset?)max);

        private async Task<CatalogIndexScan> UpdateFindPackageAssets(DateTimeOffset? max)
        {
            return await UpdateAsync(
                CatalogScanType.FindPackageAssets,
                _serializer.Serialize(new FindPackageAssetsParameters
                {
                    BucketCount = _options.Value.AppendResultStorageBucketCount,
                }).AsString(),
                max);
        }

        private async Task<CatalogIndexScan> UpdateAsync(CatalogScanType type, string parameters, DateTimeOffset? max)
        {
            // Determine the bounds of the scan.
            var cursor = await _cursorStorageService.GetOrCreateAsync($"CatalogScan-{type}");
            var index = await _catalogClient.GetCatalogIndexAsync();
            var min = new[] { cursor.Value, CatalogClient.NuGetOrgMin }.Max();
            max = max.GetValueOrDefault(index.CommitTimestamp);

            if (min == max)
            {
                return null;
            }

            // Start a new scan.
            _logger.LogInformation("Attempting to start a catalog index scan from ({Min}, {Max}].", min, max);
            var scanId = StorageUtility.GenerateDescendingId();
            var catalogIndexScanMessage = new CatalogIndexScanMessage { ScanId = scanId.ToString() };
            await _messageEnqueuer.EnqueueAsync(new[] { catalogIndexScanMessage });

            var catalogIndexScan = new CatalogIndexScan(scanId.ToString(), scanId.Unique)
            {
                ParsedScanType = type,
                ScanParameters = parameters,
                ParsedState = CatalogScanState.Created,
                Min = min,
                Max = max.Value,
                CursorName = cursor.Name,
            };
            await _catalogScanStorageService.InitializeChildTablesAsync(catalogIndexScan.StorageSuffix);
            await _catalogScanStorageService.InsertAsync(catalogIndexScan);

            return catalogIndexScan;
        }
    }
}