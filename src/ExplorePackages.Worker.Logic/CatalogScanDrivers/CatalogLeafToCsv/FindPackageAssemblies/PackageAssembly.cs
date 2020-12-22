﻿using System;

namespace Knapcode.ExplorePackages.Worker.FindPackageAssemblies
{
    public partial class PackageAssembly : PackageRecord, ICsvRecord
    {
        public PackageAssembly()
        {
        }

        public PackageAssembly(Guid? scanId, DateTimeOffset? scanTimestamp, PackageDeleteCatalogLeaf leaf)
            : base(scanId, scanTimestamp, leaf)
        {
            ResultType = PackageAssemblyResultType.Deleted;
        }

        public PackageAssembly(Guid? scanId, DateTimeOffset? scanTimestamp, PackageDetailsCatalogLeaf leaf, PackageAssemblyResultType resultType)
            : base(scanId, scanTimestamp, leaf)
        {
            ResultType = resultType;
        }

        public PackageAssemblyResultType ResultType { get; set; }

        public string Path { get; set; }
        public string Name { get; set; }
        public string AssemblyVersion { get; set; }
        public string Culture { get; set; }
        public string PublicKeyToken { get; set; }
    }
}