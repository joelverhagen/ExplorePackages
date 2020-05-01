﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Table;
using static Knapcode.ExplorePackages.Logic.Worker.TableStorageConstants;

namespace Knapcode.ExplorePackages.Logic.Worker
{
    public class LatestPackageLeafService
    {
        private readonly ServiceClientFactory _serviceClientFactory;
        private readonly ILogger<LatestPackageLeafService> _logger;

        public LatestPackageLeafService(
            ServiceClientFactory serviceClientFactory,
            ILogger<LatestPackageLeafService> logger)
        {
            _serviceClientFactory = serviceClientFactory;
            _logger = logger;
        }

        public async Task InitializeAsync()
        {
            await GetTable().CreateIfNotExistsAsync();
        }

        public async Task AddAsync(string scanId, IReadOnlyList<CatalogLeafItem> items)
        {
            var table = GetTable();
            var packageIdGroups = items.GroupBy(x => x.PackageId, StringComparer.OrdinalIgnoreCase);
            foreach (var group in packageIdGroups)
            {
                await AddAsync(table, scanId, group.Key, group);
            }
        }

        public async Task AddAsync(CloudTable table, string scanId, string packageId, IEnumerable<CatalogLeafItem> items)
        {
            // Sort items by lexicographical order, since this is what table storage does.
            var itemList = items
                .Select(x => new { Item = x, LowerVersion = GetLowerVersion(x) })
                .GroupBy(x => x.LowerVersion)
                .Select(x => x.OrderByDescending(x => x.Item.CommitTimestamp).First())
                .OrderBy(x => x.LowerVersion, StringComparer.Ordinal)
                .ToList();
            var lowerVersionToItem = itemList.ToDictionary(x => x.LowerVersion, x => x.Item);
            var lowerVersionToEtag = new Dictionary<string, string>();
            var versionsToUpsert = new List<string>();

            // Query for all of the version data in Table Storage, determining what needs to be updated.
            var lowerId = packageId.ToLowerInvariant();
            var filterString = TableQuery.CombineFilters(
                EqualScanIdAndPackageId(scanId, lowerId),
                TableOperators.And,
                TableQuery.CombineFilters(
                    GreaterThanOrEqualToVersion(itemList.First().LowerVersion),
                    TableOperators.And,
                    LessThanOrEqualToVersion(itemList.Last().LowerVersion)));
            var query = new TableQuery<LatestPackageLeaf>
            {
                FilterString = filterString,
                SelectColumns = new List<string> { RowKey, nameof(LatestPackageLeaf.CommitTimestamp) },
                TakeCount = MaxTakeCount,
            };

            TableContinuationToken token = null;
            do
            {
                var segment = await table.ExecuteQuerySegmentedAsync(query, token);
                token = segment.ContinuationToken;

                foreach (var result in segment.Results)
                {
                    if (lowerVersionToItem.TryGetValue(result.LowerVersion, out var item))
                    {
                        if (result.CommitTimestamp >= item.CommitTimestamp)
                        {
                            // The version in Table Storage is newer, ignore the version we have.
                            lowerVersionToItem.Remove(result.LowerVersion);
                        }
                        else
                        {
                            // The version in Table Storage is older, save the etag to update it.
                            lowerVersionToEtag.Add(result.LowerVersion, result.ETag);
                        }
                    }
                }
            }
            while (token != null);

            // Add the versions that remain. These are the versions that are not in Table Storage.
            versionsToUpsert.AddRange(lowerVersionToItem.Keys);
            versionsToUpsert.Sort(StringComparer.Ordinal);

            // Update or insert the rows.
            var batch = new TableBatchOperation();
            for (var i = 0; i < versionsToUpsert.Count; i++)
            {
                if (batch.Count >= MaxBatchSize)
                {
                    await ExecuteBatchAsync(table, batch);
                    batch = new TableBatchOperation();
                }

                var lowerVersion = versionsToUpsert[i];
                var leaf = lowerVersionToItem[lowerVersion];
                var entity = new LatestPackageLeaf(scanId, lowerId, lowerVersion)
                {
                    CommitTimestamp = leaf.CommitTimestamp,
                    ParsedType = leaf.Type,
                    Url = leaf.Url,
                };

                if (lowerVersionToEtag.TryGetValue(lowerVersion, out var etag))
                {
                    entity.ETag = etag;
                    batch.Add(TableOperation.Replace(entity));
                }
                else
                {
                    batch.Add(TableOperation.Insert(entity));
                }
            }

            if (batch.Count > 0)
            {
                await ExecuteBatchAsync(table, batch);
            }
        }

        private async Task ExecuteBatchAsync(CloudTable table, TableBatchOperation batch)
        {
            _logger.LogInformation("Upserting {Count} latest package leaf rows into {TableName}.", batch.Count, table.Name);
            await table.ExecuteBatchAsync(batch);
        }

        private static string EqualScanIdAndPackageId(string scanId, string lowerId)
        {
            return TableQuery.GenerateFilterCondition(
                PartitionKey,
                QueryComparisons.Equal,
                LatestPackageLeaf.GetPartitionKey(scanId, lowerId));
        }

        private static string GreaterThanOrEqualToVersion(string lowerVersion)
        {
            return TableQuery.GenerateFilterCondition(RowKey, QueryComparisons.GreaterThanOrEqual, lowerVersion);
        }

        private static string LessThanOrEqualToVersion(string lowerVersion)
        {
            return TableQuery.GenerateFilterCondition(RowKey, QueryComparisons.LessThanOrEqual, lowerVersion);
        }

        private static string GetLowerVersion(CatalogLeafItem x)
        {
            return x.ParsePackageVersion().ToNormalizedString().ToLowerInvariant();
        }

        private CloudTable GetTable()
        {
            return _serviceClientFactory
                .GetLatestPackageLeavesStorageAccount()
                .CreateCloudTableClient()
                .GetTableReference("latestleaves");
        }
    }
}