﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Knapcode.ExplorePackages.Worker.Support;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.WindowsAzure.Storage.Queue;
using Xunit;
using Xunit.Abstractions;

namespace Knapcode.ExplorePackages.Worker
{
    public abstract class BaseCatalogScanIntegrationTest : IClassFixture<DefaultWebApplicationFactory<StaticFilesStartup>>, IAsyncLifetime
    {
        public const string TestData = "TestData";
        public const string Step1 = "Step1";
        public const string Step2 = "Step2";

        private readonly Lazy<IHost> _lazyHost;

        public BaseCatalogScanIntegrationTest(
            ITestOutputHelper output,
            DefaultWebApplicationFactory<StaticFilesStartup> factory)
        {
            StoragePrefix = "t" + StorageUtility.GenerateUniqueId();
            HttpMessageHandlerFactory = new TestHttpMessageHandlerFactory();

            var currentDirectory = Directory.GetCurrentDirectory();
            var testWebHostBuilder = factory.WithWebHostBuilder(b => b
                .ConfigureLogging(b => b.SetMinimumLevel(LogLevel.Error))
                .UseContentRoot(currentDirectory)
                .UseWebRoot(currentDirectory));
            TestDataHttpClient = testWebHostBuilder.CreateClient();

            _lazyHost = new Lazy<IHost>(() => GetHost(output));
        }

        private IHost GetHost(ITestOutputHelper output)
        {
            var startup = new Startup();
            return new HostBuilder()
                .ConfigureWebJobs(startup.Configure)
                .ConfigureServices(serviceCollection =>
                {
                    serviceCollection.AddSingleton((IExplorePackagesHttpMessageHandlerFactory)HttpMessageHandlerFactory);

                    serviceCollection.AddTransient<WorkerQueueFunction>();

                    serviceCollection.AddSingleton(new TelemetryClient(TelemetryConfiguration.CreateDefault()));

                    serviceCollection.AddLogging(o =>
                    {
                        o.SetMinimumLevel(LogLevel.Trace);
                        o.AddProvider(new XunitLoggerProvider(output));
                    });

                    serviceCollection.Configure((Action<ExplorePackagesSettings>)(ConfigureDefaultsAndSettings));
                    serviceCollection.Configure((Action<ExplorePackagesWorkerSettings>)(ConfigureDefaultsAndSettings));
                })
                .Build();
        }

        private void ConfigureDefaultsAndSettings(ExplorePackagesSettings x)
        {
            x.StorageContainerName = $"{StoragePrefix}1p1";
            x.LeaseContainerName = $"{StoragePrefix}1l1";

            if (ConfigureSettings != null)
            {
                ConfigureSettings(x);
            }
        }

        private void ConfigureDefaultsAndSettings(ExplorePackagesWorkerSettings x)
        {
            x.AppendResultStorageBucketCount = 3;
            x.AppendResultUniqueIds = false;

            x.WorkerQueueName = $"{StoragePrefix}1wq1";
            x.CursorTableName = $"{StoragePrefix}1c1";
            x.CatalogIndexScanTableName = $"{StoragePrefix}1cis1";
            x.CatalogPageScanTableName = $"{StoragePrefix}1cps1";
            x.CatalogLeafScanTableName = $"{StoragePrefix}1cls1";
            x.TaskStateTableName = $"{StoragePrefix}1ts1";
            x.CatalogLeafToCsvTableName = $"{StoragePrefix}1cltc1";
            x.LatestLeavesTableName = $"{StoragePrefix}1ll1";
            x.FindPackageAssetsContainerName = $"{StoragePrefix}1fpa1";
            x.FindPackageAssembliesContainerName = $"{StoragePrefix}1fpi1";
            x.RunRealRestoreContainerName = $"{StoragePrefix}1rrr1";

            ConfigureDefaultsAndSettings((ExplorePackagesSettings)x);

            if (ConfigureWorkerSettings != null)
            {
                ConfigureWorkerSettings(x);
            }
        }

        public string StoragePrefix { get; }
        public TestHttpMessageHandlerFactory HttpMessageHandlerFactory { get; }
        public HttpClient TestDataHttpClient { get; }

        public Action<ExplorePackagesSettings> ConfigureSettings { get; set; }
        public Action<ExplorePackagesWorkerSettings> ConfigureWorkerSettings { get; set; }
        public IHost Host => _lazyHost.Value;
        public IOptions<ExplorePackagesWorkerSettings> Options => Host.Services.GetRequiredService<IOptions<ExplorePackagesWorkerSettings>>();
        public ServiceClientFactory ServiceClientFactory => Host.Services.GetRequiredService<ServiceClientFactory>();
        public IWorkerQueueFactory WorkerQueueFactory => Host.Services.GetRequiredService<IWorkerQueueFactory>();
        public CursorStorageService CursorStorageService => Host.Services.GetRequiredService<CursorStorageService>();
        public CatalogScanStorageService CatalogScanStorageService => Host.Services.GetRequiredService<CatalogScanStorageService>();
        public CatalogScanService CatalogScanService => Host.Services.GetRequiredService<CatalogScanService>();
        public ILogger Logger => Host.Services.GetRequiredService<ILogger<BaseCatalogScanIntegrationTest>>();

        protected abstract string DestinationContainerName { get; }

        protected async Task ProcessQueueAsync(CatalogIndexScan indexScan)
        {
            var queue = WorkerQueueFactory.GetQueue();
            do
            {
                CloudQueueMessage message;
                while ((message = await queue.GetMessageAsync()) != null)
                {
                    using (var scope = Host.Services.CreateScope())
                    {
                        var target = scope.ServiceProvider.GetRequiredService<WorkerQueueFunction>();
                        await target.ProcessAsync(message);
                    }

                    await queue.DeleteMessageAsync(message);
                }

                indexScan = await CatalogScanStorageService.GetIndexScanAsync(indexScan.CursorName, indexScan.ScanId);

                if (indexScan.ParsedState != CatalogScanState.Complete)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1));
                }
            }
            while (indexScan.ParsedState != CatalogScanState.Complete);
        }

        protected async Task VerifyExpectedContainersAsync()
        {
            var account = ServiceClientFactory.GetStorageAccount();

            var containers = await account.CreateCloudBlobClient().ListContainersAsync(StoragePrefix);
            Assert.Equal(
                new[] { Options.Value.LeaseContainerName, DestinationContainerName }.OrderBy(x => x).ToArray(),
                containers.Select(x => x.Name).ToArray());

            var queues = await account.CreateCloudQueueClient().ListQueuesAsync(StoragePrefix);
            Assert.Equal(
                new[] { Options.Value.WorkerQueueName, Options.Value.WorkerQueueName + "-poison" },
                queues.Select(x => x.Name).ToArray());

            var tables = await account.CreateCloudTableClient().ListTablesAsync(StoragePrefix);
            Assert.Equal(
                new[] { Options.Value.CursorTableName, Options.Value.CatalogIndexScanTableName },
                tables.Select(x => x.Name).ToArray());
        }

        protected async Task AssertOutputAsync(string testName, string stepName, int bucket)
        {
            var client = ServiceClientFactory.GetStorageAccount().CreateCloudBlobClient();
            var container = client.GetContainerReference(DestinationContainerName);
            var blob = container.GetBlockBlobReference($"compact_{bucket}.csv");
            var actual = await blob.DownloadTextAsync();
            // Directory.CreateDirectory(Path.Combine(TestData, testName, stepName));
            // File.WriteAllText(Path.Combine(TestData, testName, stepName, blob.Name), actual);
            var expected = File.ReadAllText(Path.Combine(TestData, testName, stepName, blob.Name));
            Assert.Equal(expected, actual);
        }

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            var account = ServiceClientFactory.GetStorageAccount();

            var containers = await account.CreateCloudBlobClient().ListContainersAsync(StoragePrefix);
            foreach (var container in containers)
            {
                await container.DeleteAsync();
            }

            var queues = await account.CreateCloudQueueClient().ListQueuesAsync(StoragePrefix);
            foreach (var queue in queues)
            {
                await queue.DeleteAsync();
            }

            var tables = await account.CreateCloudTableClient().ListTablesAsync(StoragePrefix);
            foreach (var table in tables)
            {
                await table.DeleteAsync();
            }
        }

        public static HttpRequestMessage Clone(HttpRequestMessage req)
        {
            var clone = new HttpRequestMessage(req.Method, req.RequestUri);

            clone.Content = req.Content;
            clone.Version = req.Version;

            foreach (KeyValuePair<string, object> prop in req.Properties)
            {
                clone.Properties.Add(prop);
            }

            foreach (KeyValuePair<string, IEnumerable<string>> header in req.Headers)
            {
                clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            return clone;
        }

        public class TestHttpMessageHandlerFactory : IExplorePackagesHttpMessageHandlerFactory
        {
            public Func<HttpRequestMessage, Task<HttpResponseMessage>> OnSendAsync { get; set; }

            public DelegatingHandler Create()
            {
                return new TestHttpMessageHandler(async req =>
                {
                    if (OnSendAsync != null)
                    {
                        return await OnSendAsync(req);
                    }

                    return null;
                });
            }
        }

        public class TestHttpMessageHandler : DelegatingHandler
        {
            private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _onSendAsync;

            public TestHttpMessageHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> onSendAsync)
            {
                _onSendAsync = onSendAsync;
            }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var response = await _onSendAsync(request);
                if (response != null)
                {
                    return response;
                }

                return await base.SendAsync(request, cancellationToken);
            }
        }
    }
}
