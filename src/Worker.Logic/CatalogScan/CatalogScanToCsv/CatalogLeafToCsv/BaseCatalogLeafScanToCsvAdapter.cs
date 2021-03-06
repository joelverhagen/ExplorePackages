﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NuGet.Insights.Worker
{
    public abstract class BaseCatalogLeafScanToCsvAdapter
    {
        private readonly SchemaSerializer _schemaSerializer;
        private readonly CsvTemporaryStorageFactory _storageFactory;
        protected readonly IReadOnlyList<ICsvTemporaryStorage> _storage;
        private readonly ICatalogLeafToCsvDriver _driver;

        public BaseCatalogLeafScanToCsvAdapter(
            SchemaSerializer schemaSerializer,
            CsvTemporaryStorageFactory storageFactory,
            IReadOnlyList<ICsvTemporaryStorage> storage,
            ICatalogLeafToCsvDriver driver)
        {
            _schemaSerializer = schemaSerializer;
            _storageFactory = storageFactory;
            _storage = storage;
            _driver = driver;
        }

        public async Task InitializeAsync(CatalogIndexScan indexScan)
        {
            foreach (var storage in _storage)
            {
                await storage.InitializeAsync(indexScan);
            }
            await _storageFactory.InitializeAsync(indexScan);
            await _driver.InitializeAsync();
        }

        public async Task StartCustomExpandAsync(CatalogIndexScan indexScan)
        {
            foreach (var storage in _storage)
            {
                await storage.StartCustomExpandAsync(indexScan);
            }
        }

        public async Task<bool> IsCustomExpandCompleteAsync(CatalogIndexScan indexScan)
        {
            foreach (var storage in _storage)
            {
                if (!await storage.IsCustomExpandCompleteAsync(indexScan))
                {
                    return false;
                }
            }

            return true;
        }

        public Task<CatalogIndexScanResult> ProcessIndexAsync(CatalogIndexScan indexScan)
        {
            var parameters = DeserializeParameters(indexScan.DriverParameters);

            CatalogIndexScanResult result;
            switch (parameters.Mode)
            {
                case CatalogLeafToCsvMode.AllLeaves:
                    result = CatalogIndexScanResult.ExpandAllLeaves;
                    break;
                case CatalogLeafToCsvMode.LatestLeaves:
                    result = _driver.SingleMessagePerId ? CatalogIndexScanResult.ExpandLatestLeavesPerId : CatalogIndexScanResult.ExpandLatestLeaves;
                    break;
                case CatalogLeafToCsvMode.Reprocess:
                    result = CatalogIndexScanResult.CustomExpand;
                    break;
                default:
                    throw new NotImplementedException();
            }

            return Task.FromResult(result);
        }

        public Task<CatalogPageScanResult> ProcessPageAsync(CatalogPageScan pageScan)
        {
            var parameters = DeserializeParameters(pageScan.DriverParameters);
            if (parameters.Mode == CatalogLeafToCsvMode.AllLeaves)
            {
                return Task.FromResult(CatalogPageScanResult.ExpandAllowDuplicates);
            }

            throw new NotSupportedException();
        }

        private CatalogLeafToCsvParameters DeserializeParameters(string driverParameters)
        {
            return (CatalogLeafToCsvParameters)_schemaSerializer.Deserialize(driverParameters).Data;
        }

        protected abstract Task<(DriverResult, IReadOnlyList<ICsvRecordSet<ICsvRecord>>)> ProcessLeafAsync(CatalogLeafItem item, int attemptCount);

        protected static T GetValueOrDefault<T>(DriverResult<T> result)
        {
            if (result.Type == DriverResultType.Success)
            {
                return result.Value;
            }

            return default;
        }

        public async Task<DriverResult> ProcessLeafAsync(CatalogLeafScan leafScan)
        {
            var leafItem = leafScan.ToLeafItem();
            (var result, var sets) = await ProcessLeafAsync(leafItem, leafScan.AttemptCount);
            if (result.Type == DriverResultType.TryAgainLater)
            {
                return result;
            }

            for (var setIndex = 0; setIndex < _storage.Count; setIndex++)
            {
                await _storage[setIndex].AppendAsync(leafScan.StorageSuffix, sets[setIndex]);
            }

            return result;
        }

        public async Task StartAggregateAsync(CatalogIndexScan indexScan)
        {
            foreach (var storage in _storage)
            {
                await storage.StartAggregateAsync(indexScan);
            }
        }

        public async Task<bool> IsAggregateCompleteAsync(CatalogIndexScan indexScan)
        {
            foreach (var storage in _storage)
            {
                if (!await storage.IsAggregateCompleteAsync(indexScan))
                {
                    return false;
                }
            }

            return true;
        }

        public async Task FinalizeAsync(CatalogIndexScan indexScan)
        {
            await _storageFactory.FinalizeAsync(indexScan);
            foreach (var storage in _storage)
            {
                await storage.FinalizeAsync(indexScan);
            }
        }
    }
}
