﻿using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Knapcode.ExplorePackages.Logic.Worker;
using Knapcode.ExplorePackages.Logic.Worker.FindPackageAssets;
using Knapcode.ExplorePackages.Logic.Worker.RunRealRestore;
using Knapcode.MiniZip;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NuGet.Configuration;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;

namespace Knapcode.ExplorePackages.Logic
{
    public static class ServiceCollectionExtensions
    {
        public const string HttpClientName = "Knapcode.ExplorePackages";
        
        public static IServiceCollection AddExplorePackagesSettings<T>(this IServiceCollection serviceCollection)
        {
            var localDirectory = Path.GetDirectoryName(typeof(T).Assembly.Location);
            return serviceCollection.AddExplorePackagesSettings(localDirectory);
        }

        public static IServiceCollection AddExplorePackagesSettings(
            this IServiceCollection serviceCollection,
            string localDirectory = null)
        {
            var userProfile = Environment.GetEnvironmentVariable("USERPROFILE") ?? Directory.GetCurrentDirectory();
            var userProfilePath = Path.Combine(userProfile, "Knapcode.ExplorePackages.Settings.json");

            var localPath = Path.Combine(
                localDirectory ?? typeof(ServiceCollectionExtensions).Assembly.Location,
                "Knapcode.ExplorePackages.Settings.json");

            var configurationBuilder = new ConfigurationBuilder()
                .AddJsonFile(userProfilePath, optional: true, reloadOnChange: false)
                .AddJsonFile(localPath, optional: true, reloadOnChange: false);

            var configuration = configurationBuilder.Build();

            serviceCollection.Configure<ExplorePackagesSettings>(configuration.GetSection(ExplorePackagesSettings.DefaultSectionName));

            return serviceCollection;
        }

        public static IServiceCollection AddExplorePackages(
            this IServiceCollection serviceCollection,
            string programName = null,
            string programVersion = null,
            string programUrl = null)
        {
            serviceCollection.AddMemoryCache();

            var userAgent = GetUserAgent(programName, programVersion, programUrl);

            serviceCollection
                .AddHttpClient(HttpClientName)
                .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
                {
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                    MaxConnectionsPerServer = 64,
                })
                .AddHttpMessageHandler<LoggingHandler>()
                .AddHttpMessageHandler<UrlReporterHandler>()
                .ConfigureHttpClient(httpClient =>
                {
                    httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);
                });
            serviceCollection.AddTransient(x => x
                .GetRequiredService<IHttpClientFactory>()
                .CreateClient(HttpClientName));

            serviceCollection.AddSingleton<ServiceClientFactory>();

            serviceCollection.AddSingleton<UrlReporterProvider>();
            serviceCollection.AddTransient<UrlReporterHandler>();
            serviceCollection.AddTransient<LoggingHandler>();
            serviceCollection.AddTransient(
                x =>
                {
                    var options = x.GetRequiredService<IOptions<ExplorePackagesSettings>>();
                    return new HttpSource(
                        new PackageSource(options.Value.V3ServiceIndex),
                        () =>
                        {
                            var factory = x.GetRequiredService<IHttpMessageHandlerFactory>();
                            var httpMessageHandler = factory.CreateHandler(HttpClientName);

                            return Task.FromResult<HttpHandlerResource>(new HttpMessageHandlerResource(httpMessageHandler));
                        },
                        NullThrottle.Instance);
                });

            serviceCollection.AddTransient(
                x => new HttpZipProvider(x.GetRequiredService<HttpClient>())
                {
                    BufferSizeProvider = new ZipBufferSizeProvider(
                        firstBufferSize: 4096,
                        secondBufferSize: 4096,
                        exponent: 2)
                });
            serviceCollection.AddTransient<MZipFormat>();

            serviceCollection.AddTransient<NuspecStore>();
            serviceCollection.AddTransient<MZipStore>();
            serviceCollection.AddTransient<IPortTester, PortTester>();
            serviceCollection.AddTransient<IPortDiscoverer, SimplePortDiscoverer>();
            serviceCollection.AddTransient<SearchServiceCursorReader>();
            serviceCollection.AddTransient<IProgressReporter, NullProgressReporter>();
            serviceCollection.AddTransient<LatestCatalogCommitFetcher>();
            serviceCollection.AddTransient<PackageBlobNameProvider>();
            serviceCollection.AddTransient<IFileStorageService, FileStorageService>();
            serviceCollection.AddTransient<IBlobStorageService, BlobStorageService>();

            serviceCollection.AddTransient<IRawMessageEnqueuer, QueueStorageEnqueuer>();
            serviceCollection.AddTransient<IWorkerQueueFactory, UnencodedWorkerQueueFactory>();

            serviceCollection.AddSingleton<IBatchSizeProvider, BatchSizeProvider>();
            serviceCollection.AddTransient<CommitEnumerator>();

            serviceCollection.AddTransient<V2Parser>();
            serviceCollection.AddSingleton<ServiceIndexCache>();
            serviceCollection.AddTransient<GalleryClient>();
            serviceCollection.AddTransient<V2Client>();
            serviceCollection.AddTransient<PackagesContainerClient>();
            serviceCollection.AddTransient<FlatContainerClient>();
            serviceCollection.AddTransient<RegistrationClient>();
            serviceCollection.AddTransient<SearchClient>();
            serviceCollection.AddTransient<AutocompleteClient>();
            serviceCollection.AddTransient<IPackageDownloadsClient, PackageDownloadsClient>();
            serviceCollection.AddTransient<CatalogClient>();

            serviceCollection.AddTransient<PackageConsistencyContextBuilder>();
            serviceCollection.AddTransient<GalleryConsistencyService>();
            serviceCollection.AddTransient<V2ConsistencyService>();
            serviceCollection.AddTransient<FlatContainerConsistencyService>();
            serviceCollection.AddTransient<PackagesContainerConsistencyService>();
            serviceCollection.AddTransient<RegistrationOriginalConsistencyService>();
            serviceCollection.AddTransient<RegistrationGzippedConsistencyService>();
            serviceCollection.AddTransient<RegistrationSemVer2ConsistencyService>();
            serviceCollection.AddTransient<SearchConsistencyService>();
            serviceCollection.AddTransient<PackageConsistencyService>();
            serviceCollection.AddTransient<CrossCheckConsistencyService>();

            serviceCollection.AddTransient<GenericMessageProcessor>();
            serviceCollection.AddTransient<SchemaSerializer>();
            serviceCollection.AddTransient<MessageEnqueuer>();

            serviceCollection.AddTransient<IMessageProcessor<BulkEnqueueMessage>, BulkEnqueueMessageProcessor>();
            serviceCollection.AddTransient<IMessageProcessor<CatalogIndexScanMessage>, CatalogIndexScanMessageProcessor>();
            serviceCollection.AddTransient<IMessageProcessor<CatalogPageScanMessage>, CatalogPageScanMessageProcessor>();
            serviceCollection.AddTransient<IMessageProcessor<CatalogLeafScanMessage>, CatalogLeafScanMessageProcessor>();
            serviceCollection.AddTransient<IMessageProcessor<FindPackageAssetsCompactMessage>, FindPackageAssetsCompactProcessor>();
            serviceCollection.AddTransient<IMessageProcessor<RunRealRestoreMessage>, RunRealRestoreProcessor>();
            serviceCollection.AddTransient<IMessageProcessor<RunRealRestoreCompactMessage>, RunRealRestoreCompactProcessor>();

            serviceCollection.AddTransient<CatalogScanStorageService>();
            serviceCollection.AddTransient<LatestPackageLeafStorageService>();
            serviceCollection.AddTransient<CursorStorageService>();

            serviceCollection.AddTransient<AppendResultStorageService>();
            serviceCollection.AddTransient<TaskStateStorageService>();

            serviceCollection.AddTransient<ProjectHelper>();

            serviceCollection.AddTransient<CatalogScanDriverFactory>();
            serviceCollection.AddTransient<DownloadLeavesCatalogScanDriver>();
            serviceCollection.AddTransient<DownloadPagesCatalogScanDriver>();
            serviceCollection.AddTransient<FindLatestLeavesCatalogScanDriver>();
            serviceCollection.AddTransient<FindPackageAssetsScanDriver>();

            serviceCollection.AddTransient<CatalogScanService>();

            return serviceCollection;
        }

        private static string GetUserAgent(string programName, string programVersion, string programUrl)
        {
            var builder = new StringBuilder();

            builder.Append(new UserAgentStringBuilder("NuGet Test Client")
                .WithOSDescription(RuntimeInformation.OSDescription)
                .Build());

            builder.Append(" ");
            if (string.IsNullOrWhiteSpace(programName))
            {
                builder.Append("Knapcode.ExplorePackages");
            }
            else
            {
                builder.Append(programName);
            }
            builder.Append(".Bot");

            if (string.IsNullOrWhiteSpace(programVersion))
            {
                var assemblyInformationalVersion = typeof(ServiceCollectionExtensions)
                    .Assembly
                    .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                    .InformationalVersion;
                if (assemblyInformationalVersion != null)
                {
                    builder.Append("/");
                    builder.Append(assemblyInformationalVersion);
                }
            }
            else
            {
                builder.Append("/");
                builder.Append(programVersion);
            }

            builder.Append(" (");
            builder.Append(RuntimeInformation.OSArchitecture);
            builder.Append("; ");
            builder.Append(RuntimeInformation.ProcessArchitecture);
            builder.Append("; ");
            builder.Append(RuntimeInformation.FrameworkDescription);
            builder.Append("; +");
            if (string.IsNullOrWhiteSpace(programUrl))
            {
                builder.Append("https://github.com/joelverhagen/ExplorePackages");
            }
            else
            {
                builder.Append(programUrl);
            }
            builder.Append(")");

            return builder.ToString();
        }
    }
}
