﻿using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Knapcode.ExplorePackages.Logic;
using Knapcode.ExplorePackages.Logic.Worker;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Table;
using static Knapcode.ExplorePackages.Logic.Worker.TableStorageConstants;

namespace Knapcode.ExplorePackages.Tool
{
    public class SandboxCommand : ICommand
    {
        private readonly ServiceClientFactory _serviceClientFactory;
        private readonly MessageEnqueuer _messageEnqueuer;
        private readonly CatalogScanStorageService _catalogScanStorageService;
        private readonly LatestPackageLeafService _latestPackageLeafService;
        private readonly IMessageProcessor<CatalogIndexScanMessage> _catalogIndexScanMessageProcessor;
        private readonly ILogger<SandboxCommand> _logger;

        public SandboxCommand(
            ServiceClientFactory serviceClientFactory,
            MessageEnqueuer messageEnqueuer,
            CatalogScanStorageService catalogScanStorageService,
            LatestPackageLeafService latestPackageLeafService,
            IMessageProcessor<CatalogIndexScanMessage> catalogIndexScanMessageProcessor,
            ILogger<SandboxCommand> logger)
        {
            _serviceClientFactory = serviceClientFactory;
            _messageEnqueuer = messageEnqueuer;
            _catalogScanStorageService = catalogScanStorageService;
            _latestPackageLeafService = latestPackageLeafService;
            _catalogIndexScanMessageProcessor = catalogIndexScanMessageProcessor;
            _logger = logger;
        }

        public void Configure(CommandLineApplication app)
        {
        }

        public async Task ExecuteAsync(CancellationToken token)
        {
            await _catalogScanStorageService.InitializeAsync();
            await _latestPackageLeafService.InitializeAsync();

            _logger.LogInformation("Clearing queues and tables...");
            await _serviceClientFactory.GetStorageAccount().CreateCloudQueueClient().GetQueueReference("queue").ClearAsync();
            await _serviceClientFactory.GetStorageAccount().CreateCloudQueueClient().GetQueueReference("queue-poison").ClearAsync();
            await _serviceClientFactory.GetStorageAccount().CreateCloudQueueClient().GetQueueReference("test").ClearAsync();
            await _serviceClientFactory.GetStorageAccount().CreateCloudQueueClient().GetQueueReference("test-poison").ClearAsync();
            await DeleteAllRowsAsync(_serviceClientFactory.GetLatestPackageLeavesStorageAccount().CreateCloudTableClient().GetTableReference("catalogindexscans"));
            await DeleteAllRowsAsync(_serviceClientFactory.GetLatestPackageLeavesStorageAccount().CreateCloudTableClient().GetTableReference("catalogpagescans"));
            await DeleteAllRowsAsync(_serviceClientFactory.GetLatestPackageLeavesStorageAccount().CreateCloudTableClient().GetTableReference("latestleaves"));

            var descendingComponent = (long.MaxValue - DateTimeOffset.UtcNow.Ticks).ToString("D20");
            var uniqueComponent = Guid.NewGuid().ToString("N");
            var scanId = descendingComponent + "-" + uniqueComponent;

            var catalogIndexScanMessage = new CatalogIndexScanMessage { ScanId = scanId };
            await _messageEnqueuer.EnqueueAsync(new[] { catalogIndexScanMessage });

            await _catalogScanStorageService.CreateIndexScanAsync(new CatalogIndexScan(scanId)
            {
                ParsedScanType = CatalogScanType.FindLatestLeaves,
                ParsedState = CatalogIndexScanState.Created,
            });
        }

        private async Task DeleteAllRowsAsync(CloudTable table)
        {
            TableContinuationToken token = null;
            do
            {
                var queryResult = await table.ExecuteQuerySegmentedAsync(new TableQuery(), token);
                token = queryResult.ContinuationToken;

                if (!queryResult.Results.Any())
                {
                    continue;
                }

                var partitionKeyGroups = new ConcurrentBag<IGrouping<string, DynamicTableEntity>>(queryResult.Results.GroupBy(x => x.PartitionKey));
                var maxKeyLength = partitionKeyGroups.Max(x => x.Key.Length);

                var workers = Enumerable
                    .Range(0, 32)
                    .Select(async x =>
                    {
                        while (partitionKeyGroups.TryTake(out var group))
                        {
                            var batch = new TableBatchOperation();
                            foreach (var row in group)
                            {
                                if (batch.Count >= MaxBatchSize)
                                {
                                    await ExecuteBatch(table, group.Key, batch);
                                    batch = new TableBatchOperation();
                                }

                                batch.Add(TableOperation.Delete(row));
                            }

                            if (batch.Count > 0)
                            {
                                await ExecuteBatch(table, group.Key.PadRight(maxKeyLength), batch);
                            }
                        }
                    })
                    .ToList();
                await Task.WhenAll(workers);
            }
            while (token != null);
        }

        private async Task ExecuteBatch(CloudTable table, string partitionKey, TableBatchOperation batch)
        {
            _logger.LogInformation("[ {TableName}, {PartitionKey} ] Deleting batch of {Count} rows...", table.Name, partitionKey, batch.Count);
            await table.ExecuteBatchAsync(batch);
        }

        public bool IsInitializationRequired() => true;
        public bool IsDatabaseRequired() => true;
        public bool IsSingleton() => false;

    }
}
