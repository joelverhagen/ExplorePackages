﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.WindowsAzure.Storage.Table;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace Knapcode.ExplorePackages.WideEntities
{
    public class WideEntityServiceTest : IClassFixture<WideEntityServiceTest.Fixture>
    {
        [Theory]
        [MemberData(nameof(RoundTripsTestData))]
        public async Task RoundTrips(int length)
        {
            // Arrange
            var src = Bytes.Slice(0, length);
            var partitionKey = nameof(RoundTrips) + length;
            var rowKey = "foo";

            // Act
            await Target.InsertAsync(TableName, partitionKey, rowKey, src);
            using var srcStream = await Target.GetAsync(TableName, partitionKey, rowKey);

            // Assert
            using var destStream = new MemoryStream();
            await srcStream.CopyToAsync(destStream);

            Assert.Equal(src.ToArray(), destStream.ToArray());
        }

        public static IEnumerable<object[]> RoundTripsTestData => ByteArrayLengths
            .Distinct()
            .OrderBy(x => x)
            .Select(x => new object[] { x });

        private static IEnumerable<int> ByteArrayLengths
        {
            get
            {
                yield return 0;
                var current = 1;
                do
                {
                    yield return current;
                    current *= 2;
                }
                while (current <= WideEntityService.MaxTotalEntitySize);

                for (var i = 16; i >= 0; i--)
                {
                    yield return WideEntityService.MaxTotalEntitySize;
                }

                var random = new Random(0);
                for (var i = 0; i < 26; i++)
                {
                    yield return random.Next(0, WideEntityService.MaxTotalEntitySize);
                }
            }
        }

        private readonly Fixture _fixture;

        public WideEntityServiceTest(Fixture fixture, ITestOutputHelper output)
        {
            _fixture = fixture;
            Target = new WideEntityService(_fixture.ServiceClientFactory);
        }

        public string TableName => _fixture.TableName;
        public ReadOnlyMemory<byte> Bytes => _fixture.Bytes.AsMemory();
        public WideEntityService Target { get; }

        public class Fixture : IAsyncLifetime
        {
            public Fixture()
            {
                Options = new Mock<IOptions<ExplorePackagesSettings>>();
                Settings = new ExplorePackagesSettings();
                Options.Setup(x => x.Value).Returns(() => Settings);
                ServiceClientFactory = new ServiceClientFactory(Options.Object);
                TableName = "t" + StorageUtility.GenerateUniqueId().ToLowerInvariant();

                Bytes = new byte[4 * 1024 * 1024];
                var random = new Random(0);
                random.NextBytes(Bytes);
            }

            public Mock<IOptions<ExplorePackagesSettings>> Options { get; }
            public ExplorePackagesSettings Settings { get; }
            public ServiceClientFactory ServiceClientFactory { get; }
            public string TableName { get; }
            public byte[] Bytes { get; }

            public Task InitializeAsync() => GetTable().CreateIfNotExistsAsync();
            public Task DisposeAsync() => Task.CompletedTask;

            private CloudTable GetTable()
            {
                return ServiceClientFactory.GetStorageAccount().CreateCloudTableClient().GetTableReference(TableName);
            }
        }
    }
}
