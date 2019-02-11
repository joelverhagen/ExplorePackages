﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Knapcode.ExplorePackages.Entities;

namespace Knapcode.ExplorePackages.Logic
{
    public class DependenciesToDatabaseCommitProcessor : ICommitProcessor<PackageEntity, PackageDependencyGroups, object>
    {
        private readonly NuspecStore _nuspecStore;
        private readonly PackageDependencyService _packageDependencyService;
        private readonly IBatchSizeProvider _batchSizeProvider;

        public DependenciesToDatabaseCommitProcessor(
            NuspecStore nuspecStore,
            PackageDependencyService packageDependencyService,
            IBatchSizeProvider batchSizeProvider)
        {
            _nuspecStore = nuspecStore;
            _packageDependencyService = packageDependencyService;
            _batchSizeProvider = batchSizeProvider;
        }

        public string CursorName => CursorNames.DependenciesToDatabase;

        public IReadOnlyList<string> DependencyCursorNames { get; } = new[]
        {
            CursorNames.CatalogToNuspecs,
            CursorNames.CatalogToDatabase,
        };

        public int BatchSize => _batchSizeProvider.Get(BatchSizeType.DependenciesToDatabase);

        public string SerializeProgressToken(object progressToken)
        {
            throw new NotImplementedException();
        }

        public object DeserializeProgressToken(string serializedProgressToken)
        {
            throw new NotImplementedException();
        }

        public async Task<ItemBatch<PackageDependencyGroups, object>> InitializeItemsAsync(
            IReadOnlyList<PackageEntity> packages,
            object progressToken,
            CancellationToken token)
        {
            var output = await TaskProcessor.ExecuteAsync(
                packages,
                x => InitializeItemAsync(x),
                workerCount: 32,
                token: token);

            var list = output.Where(x => x != null).ToList();

            return new ItemBatch<PackageDependencyGroups, object>(list);
        }

        private async Task<PackageDependencyGroups> InitializeItemAsync(PackageEntity package)
        {
            var nuspec = await _nuspecStore.GetNuspecContextAsync(package.PackageRegistration.Id, package.Version);
            if (nuspec.Document == null)
            {
                return null;
            }

            var identity = new PackageIdentity(package.PackageRegistration.Id, package.Version);
            var dependencyGroups = NuspecUtility.GetParsedDependencyGroups(nuspec.Document);
            var packageDependencyGroups = new PackageDependencyGroups(identity, dependencyGroups);

            return packageDependencyGroups;
        }

        public async Task ProcessBatchAsync(IReadOnlyList<PackageDependencyGroups> batch)
        {
            await _packageDependencyService.AddDependenciesAsync(batch);
        }
    }
}
