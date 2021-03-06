// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NuGet.Insights.Worker.BuildVersionSet;
using NuGet.Insights.Worker.CatalogDataToCsv;
using NuGet.Insights.Worker.CatalogLeafItemToCsv;
using NuGet.Insights.Worker.FindLatestCatalogLeafScanPerId;
using NuGet.Insights.Worker.LoadLatestPackageLeaf;
using NuGet.Insights.Worker.LoadPackageArchive;
using NuGet.Insights.Worker.LoadPackageManifest;
using NuGet.Insights.Worker.LoadPackageVersion;
#if ENABLE_NPE
using NuGet.Insights.Worker.NuGetPackageExplorerToCsv;
#endif
using NuGet.Insights.Worker.PackageArchiveToCsv;
using NuGet.Insights.Worker.PackageAssemblyToCsv;
using NuGet.Insights.Worker.PackageAssetToCsv;
using NuGet.Insights.Worker.PackageManifestToCsv;
using NuGet.Insights.Worker.PackageSignatureToCsv;
using NuGet.Insights.Worker.PackageVersionToCsv;

namespace NuGet.Insights.Worker
{
    public class CatalogScanDriverFactory : ICatalogScanDriverFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IOptions<NuGetInsightsWorkerSettings> _options;

        public CatalogScanDriverFactory(IServiceProvider serviceProvider, IOptions<NuGetInsightsWorkerSettings> options)
        {
            _serviceProvider = serviceProvider;
            _options = options;
        }

        public ICatalogScanDriver Create(CatalogScanDriverType driverType)
        {
            return (ICatalogScanDriver)CreateBatchDriverOrNull(driverType) ?? CreateNonBatchDriver(driverType);
        }

        public ICatalogLeafScanBatchDriver CreateBatchDriverOrNull(CatalogScanDriverType driverType)
        {
            switch (driverType)
            {
                case CatalogScanDriverType.LoadPackageArchive:
                    return _serviceProvider.GetRequiredService<LoadPackageArchiveDriver>();
                case CatalogScanDriverType.LoadPackageManifest:
                    return _serviceProvider.GetRequiredService<LoadPackageManifestDriver>();
                case CatalogScanDriverType.LoadPackageVersion:
                    return _serviceProvider.GetRequiredService<LoadPackageVersionDriver>();
                default:
                    if (_options.Value.RunAllCatalogScanDriversAsBatch)
                    {
                        return WrapNonBatchDriver(CreateNonBatchDriver(driverType));
                    }
                    else
                    {
                        return null;
                    }
            }
        }

        public ICatalogLeafScanNonBatchDriver CreateNonBatchDriver(CatalogScanDriverType driverType)
        {
            switch (driverType)
            {
                case CatalogScanDriverType.Internal_FindLatestCatalogLeafScan:
                    return _serviceProvider.GetRequiredService<FindLatestLeafDriver<CatalogLeafScan>>();
                case CatalogScanDriverType.Internal_FindLatestCatalogLeafScanPerId:
                    return _serviceProvider.GetRequiredService<FindLatestLeafDriver<CatalogLeafScanPerId>>();

                case CatalogScanDriverType.BuildVersionSet:
                    return _serviceProvider.GetRequiredService<BuildVersionSetDriver>();
                case CatalogScanDriverType.CatalogLeafItemToCsv:
                    return _serviceProvider.GetRequiredService<CatalogLeafItemToCsvDriver>();
                case CatalogScanDriverType.LoadLatestPackageLeaf:
                    return _serviceProvider.GetRequiredService<FindLatestLeafDriver<LatestPackageLeaf>>();
                case CatalogScanDriverType.PackageArchiveToCsv:
                    return _serviceProvider.GetRequiredService<CatalogLeafScanToCsvAdapter<PackageArchiveRecord, PackageArchiveEntry>>();
                case CatalogScanDriverType.PackageAssemblyToCsv:
                    return _serviceProvider.GetRequiredService<CatalogLeafScanToCsvAdapter<PackageAssembly>>();
                case CatalogScanDriverType.PackageAssetToCsv:
                    return _serviceProvider.GetRequiredService<CatalogLeafScanToCsvAdapter<PackageAsset>>();
                case CatalogScanDriverType.PackageSignatureToCsv:
                    return _serviceProvider.GetRequiredService<CatalogLeafScanToCsvAdapter<PackageSignature>>();
                case CatalogScanDriverType.PackageManifestToCsv:
                    return _serviceProvider.GetRequiredService<CatalogLeafScanToCsvAdapter<PackageManifestRecord>>();
                case CatalogScanDriverType.PackageVersionToCsv:
                    return _serviceProvider.GetRequiredService<CatalogLeafScanToCsvAdapter<PackageVersionRecord>>();
#if ENABLE_NPE
                case CatalogScanDriverType.NuGetPackageExplorerToCsv:
                    return _serviceProvider.GetRequiredService<CatalogLeafScanToCsvAdapter<NuGetPackageExplorerRecord, NuGetPackageExplorerFile>>();
#endif
                case CatalogScanDriverType.CatalogDataToCsv:
                    return _serviceProvider.GetRequiredService<CatalogLeafScanToCsvAdapter<PackageDeprecationRecord, PackageVulnerabilityRecord>>();
                default:
                    throw new NotSupportedException($"Catalog scan driver type '{driverType}' is not supported.");
            }
        }

        private ICatalogLeafScanBatchDriver WrapNonBatchDriver(ICatalogLeafScanNonBatchDriver driver)
        {
            return new CatalogLeafScanBatchDriverAdapter(
                driver,
                _serviceProvider.GetRequiredService<ILogger<CatalogLeafScanBatchDriverAdapter>>());
        }
    }
}
