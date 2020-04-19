﻿using Knapcode.ExplorePackages.Logic;
using Knapcode.ExplorePackages.Logic.Worker;
using Knapcode.ExplorePackages.Worker;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host.Queues;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(Startup))]

namespace Knapcode.ExplorePackages.Worker
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder
                .Services
                .AddOptions<ExplorePackagesSettings>()
                .Configure<IConfiguration>((settings, configuration) =>
                {
                    configuration.GetSection(ExplorePackagesSettings.DefaultSectionName).Bind(settings);
                });

            builder.Services.AddExplorePackages();

            builder.Services.AddSingleton<IQueueProcessorFactory, UnencodedQueueProcessorFactory>();
            builder.Services.AddScoped<UnencodedCloudQueueEnqueuer>();
            builder.Services.AddTransient<IRawMessageEnqueuer>(s => s.GetRequiredService<UnencodedCloudQueueEnqueuer>());
        }
    }
}
