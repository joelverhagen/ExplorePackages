﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace NuGet.Insights.Worker.CatalogLeafItemToCsv
{
    public class CatalogLeafItemToCsvIntegrationTest : BaseCatalogScanToCsvIntegrationTest<CatalogLeafItemRecord>
    {
        private const string CatalogLeafItemToCsvDir = nameof(CatalogLeafItemToCsv);
        private const string CatalogLeafItemToCsv_WithDuplicatesDir = nameof(CatalogLeafItemToCsv_WithDuplicates);

        public class CatalogLeafItemToCsv : CatalogLeafItemToCsvIntegrationTest
        {
            public CatalogLeafItemToCsv(ITestOutputHelper output, DefaultWebApplicationFactory<StaticFilesStartup> factory)
                : base(output, factory)
            {
            }

            [Fact]
            public async Task Execute()
            {
                ConfigureWorkerSettings = x => x.AppendResultStorageBucketCount = 1;

                // Arrange
                var min0 = DateTimeOffset.Parse("2020-12-27T05:06:30.4180312Z");
                var max1 = DateTimeOffset.Parse("2020-12-27T05:07:45.7628472Z");

                await CatalogScanService.InitializeAsync();
                await SetCursorAsync(min0);

                // Act
                await UpdateAsync(max1);

                // Assert
                await AssertOutputAsync(CatalogLeafItemToCsvDir, Step1, 0);
            }
        }

        public class CatalogLeafItemToCsv_WithDuplicates : CatalogLeafItemToCsvIntegrationTest
        {
            public CatalogLeafItemToCsv_WithDuplicates(ITestOutputHelper output, DefaultWebApplicationFactory<StaticFilesStartup> factory)
                : base(output, factory)
            {
            }

            [Fact]
            public async Task Execute()
            {
                ConfigureWorkerSettings = x => x.AppendResultStorageBucketCount = 1;

                // Arrange
                var min0 = DateTimeOffset.Parse("2020-11-27T21:58:12.5094058Z");
                var max1 = DateTimeOffset.Parse("2020-11-27T22:09:56.3587144Z");

                await CatalogScanService.InitializeAsync();
                await SetCursorAsync(min0);

                // Act
                await UpdateAsync(max1);

                // Assert
                await AssertOutputAsync(CatalogLeafItemToCsv_WithDuplicatesDir, Step1, 0);
            }
        }

        public CatalogLeafItemToCsvIntegrationTest(ITestOutputHelper output, DefaultWebApplicationFactory<StaticFilesStartup> factory)
            : base(output, factory)
        {
        }

        protected override string DestinationContainerName => Options.Value.CatalogLeafItemContainerName;
        protected override CatalogScanDriverType DriverType => CatalogScanDriverType.CatalogLeafItemToCsv;
        public override IEnumerable<CatalogScanDriverType> LatestLeavesTypes => Enumerable.Empty<CatalogScanDriverType>();
        public override IEnumerable<CatalogScanDriverType> LatestLeavesPerIdTypes => Enumerable.Empty<CatalogScanDriverType>();
    }
}
