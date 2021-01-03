﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace Knapcode.ExplorePackages.Worker.FindLatestCatalogLeafScans
{
    public class FindLatestCatalogLeafScansIntegrationTest : BaseCatalogScanIntegrationTest
    {
        private const string FindLatestCatalogLeafScansDir = nameof(FindLatestCatalogLeafScans);
        private const string FindLatestCatalogLeafScans_WithDuplicatesDir = nameof(FindLatestCatalogLeafScans_WithDuplicates);

        private const string ParentStorageSuffix = "parentstoragesuffix";

        public FindLatestCatalogLeafScansIntegrationTest(ITestOutputHelper output, DefaultWebApplicationFactory<StaticFilesStartup> factory)
            : base(output, factory)
        {
        }

        protected override CatalogScanDriverType DriverType => CatalogScanDriverType.FindLatestCatalogLeafScans;

        public class FindLatestCatalogLeafScans : FindLatestCatalogLeafScansIntegrationTest
        {
            public FindLatestCatalogLeafScans(ITestOutputHelper output, DefaultWebApplicationFactory<StaticFilesStartup> factory)
                : base(output, factory)
            {
            }

            [Fact]
            public async Task Execute()
            {
                Logger.LogInformation("Settings: " + Environment.NewLine + JsonConvert.SerializeObject(Options.Value, Formatting.Indented));

                // Arrange
                var min0 = DateTimeOffset.Parse("2020-12-27T05:06:30.4180312Z");
                var max1 = DateTimeOffset.Parse("2020-12-27T05:07:21.9968244Z");

                await CatalogScanService.InitializeAsync();

                // Act
                await UpdateAsync(min0, max1);

                // Assert
                await VerifyOutputAsync(FindLatestCatalogLeafScansDir);
            }
        }

        public class FindLatestCatalogLeafScans_WithDuplicates : FindLatestCatalogLeafScansIntegrationTest
        {
            public FindLatestCatalogLeafScans_WithDuplicates(ITestOutputHelper output, DefaultWebApplicationFactory<StaticFilesStartup> factory)
                : base(output, factory)
            {
            }

            [Fact]
            public async Task Execute()
            {
                Logger.LogInformation("Settings: " + Environment.NewLine + JsonConvert.SerializeObject(Options.Value, Formatting.Indented));

                // Arrange
                var min0 = DateTimeOffset.Parse("2020-11-27T21:58:12.5094058Z");
                var max1 = DateTimeOffset.Parse("2020-11-27T22:09:56.3587144Z");

                await CatalogScanService.InitializeAsync();

                // Act
                await UpdateAsync(min0, max1);

                // Assert
                await VerifyOutputAsync(FindLatestCatalogLeafScans_WithDuplicatesDir);
            }
        }

        protected override IEnumerable<string> GetExpectedTableNames()
        {
            yield return Options.Value.CatalogLeafScanTableName + ParentStorageSuffix;
        }

        protected override IEnumerable<string> GetExpectedCursorNames()
        {
            yield break;
        }

        private async Task UpdateAsync(DateTimeOffset min0, DateTimeOffset max1)
        {
            // Arrange
            var parentScan = new CatalogIndexScan("parent-cursor", "parent-scan-id", ParentStorageSuffix)
            {
                ParsedDriverType = CatalogScanDriverType.FindPackageAssets,
                DriverParameters = "parent-parameters",
                ParsedState = CatalogScanState.Created,
                Min = DateTimeOffset.Parse("2020-01-01T00:00:00Z"),
                Max = DateTimeOffset.Parse("2020-01-02T00:00:00Z"),
            };

            await CatalogScanStorageService.InsertAsync(parentScan);
            ExpectedCatalogIndexScans.Add(parentScan);

            await GetLeafScanTable().CreateIfNotExistsAsync();

            var scan = await CatalogScanService.GetOrStartFindLatestCatalogLeafScansAsync(
                scanId: "flcls-scan-id",
                storageSuffix: "flclsstoragesuffix",
                parentScanMessage: new CatalogIndexScanMessage
                {
                    CursorName = parentScan.CursorName,
                    ScanId = parentScan.ScanId,
                },
                min0,
                max1);

            // Act
            await UpdateAsync(scan);
        }

        private async Task VerifyOutputAsync(string dir)
        {
            await VerifyOutputAsync<CatalogLeafScan>(
                GetLeafScanTable(),
                dir,
                cleanEntity: x => x.Created = DateTimeOffset.Parse("2020-01-03T00:00:00Z"));
        }

        private CloudTable GetLeafScanTable()
        {
            return CatalogScanStorageService.GetLeafScanTable(ParentStorageSuffix);
        }
    }
}
