// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace NuGet.Insights.Worker.PackageArchiveToCsv
{
    public partial record PackageArchiveRecord : PackageRecord, ICsvRecord
    {
        public PackageArchiveRecord()
        {
        }

        public PackageArchiveRecord(Guid scanId, DateTimeOffset scanTimestamp, PackageDeleteCatalogLeaf leaf)
            : base(scanId, scanTimestamp, leaf)
        {
            ResultType = PackageArchiveResultType.Deleted;
        }

        public PackageArchiveRecord(Guid scanId, DateTimeOffset scanTimestamp, PackageDetailsCatalogLeaf leaf)
            : base(scanId, scanTimestamp, leaf)
        {
            ResultType = PackageArchiveResultType.Available;
        }

        public PackageArchiveResultType ResultType { get; set; }

        public long Size { get; set; }
        public string MD5 { get; set; }
        public string SHA1 { get; set; }
        public string SHA256 { get; set; }
        public string SHA512 { get; set; }

        public long OffsetAfterEndOfCentralDirectory { get; set; }
        public uint CentralDirectorySize { get; set; }
        public uint OffsetOfCentralDirectory { get; set; }
        public int EntryCount { get; set; }
        public string Comment { get; set; }
    }
}
