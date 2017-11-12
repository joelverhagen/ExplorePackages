﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Knapcode.ExplorePackages.Commands;
using Knapcode.ExplorePackages.Entities;
using Knapcode.ExplorePackages.Support;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using NuGet.Common;
using NuGet.Protocol.Core.Types;

namespace Knapcode.ExplorePackages
{
    public class Program
    {
        public static void Main(string[] args)
        {
            MainAsync(args, CancellationToken.None).GetAwaiter().GetResult();
        }

        private static async Task MainAsync(string[] args, CancellationToken token)
        {
            // Read and show the settings
            Console.WriteLine("===== settings =====");
            var settings = ReadSettingsFromDisk() ?? new ExplorePackagesSettings();
            Console.WriteLine(JsonConvert.SerializeObject(settings, Formatting.Indented));
            Console.WriteLine("====================");
            Console.WriteLine();

            // Initialize the dependency injection container.
            var serviceCollection = InitializeServiceCollection(settings);
            using (var serviceProvider = serviceCollection.BuildServiceProvider())
            {
                // Determine the commands to run.
                var log = serviceProvider.GetRequiredService<ILogger>();
                if (args.Length == 0)
                {
                    log.LogError("You must provide a parameter.");
                    return;
                }

                var commands = new List<ICommand>();
                switch (args[0].Trim().ToLowerInvariant())
                {
                    case "nuspecqueries":
                        commands.Add(serviceProvider.GetRequiredService<PackageQueriesCommand>());
                        break;
                    case "fetchcursors":
                        commands.Add(serviceProvider.GetRequiredService<FetchCursorsCommand>());
                        break;
                    case "catalogtodatabase":
                        commands.Add(serviceProvider.GetRequiredService<CatalogToDatabaseCommand>());
                        break;
                    case "catalogtonuspecs":
                        commands.Add(serviceProvider.GetRequiredService<CatalogToNuspecsCommand>());
                        break;
                    case "showqueryresults":
                        commands.Add(serviceProvider.GetRequiredService<ShowQueryResultsCommand>());
                        break;
                    case "showrepositories":
                        commands.Add(serviceProvider.GetRequiredService<ShowRepositoriesCommand>());
                        break;
                    case "checkpackage":
                        commands.Add(serviceProvider.GetRequiredService<CheckPackageCommand>());
                        break;
                    case "update":
                        commands.Add(serviceProvider.GetRequiredService<FetchCursorsCommand>());
                        commands.Add(serviceProvider.GetRequiredService<CatalogToDatabaseCommand>());
                        commands.Add(serviceProvider.GetRequiredService<CatalogToNuspecsCommand>());
                        commands.Add(serviceProvider.GetRequiredService<PackageQueriesCommand>());
                        break;
                    default:
                        log.LogError("Unknown command.");
                        return;
                }
                
                // Execute.
                var initializeDatabase = commands.Any(x => x.IsDatabaseRequired(args));
                await InitializeGlobalState(settings, initializeDatabase, log);
                foreach (var command in commands)
                {
                    var commandName = command.GetType().Name;
                    var suffix = "Command";
                    if (commandName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                    {
                        commandName = commandName.Substring(0, commandName.Length - suffix.Length);
                    }
                    var heading = $"===== {commandName.ToLowerInvariant()} =====";
                    Console.WriteLine(heading);
                    await command.ExecuteAsync(args, token);
                    Console.WriteLine(new string('=', heading.Length));
                    Console.WriteLine();
                }
            }
        }
        
        private static async Task InitializeGlobalState(ExplorePackagesSettings settings, bool initializeDatabase, ILogger log)
        {
            Console.WriteLine("===== initialize =====");
            // Initialize the database.
            EntityContext.ConnectionString = "Data Source=" + settings.DatabasePath;
            EntityContext.Enabled = initializeDatabase;
            if (initializeDatabase)
            {
                using (var entityContext = new EntityContext())
                {
                    if (!File.Exists(settings.DatabasePath))
                    {
                        log.LogInformation($"The database does not yet exist and will be created.");
                        await entityContext.Database.EnsureCreatedAsync();
                        log.LogInformation($"The database now exists.");
                    }
                    else
                    {
                        log.LogInformation($"The database already exists.");
                    }
                }
            }
            else
            {
                log.LogInformation("The database will not be used.");
            }

            // Allow up to 32 parallel HTTP connections.
            ServicePointManager.DefaultConnectionLimit = 32;

            // Set the user agent.
            var userAgentStringBuilder = new UserAgentStringBuilder("Knapcode.ExplorePackages.Bot");
            UserAgent.SetUserAgentString(userAgentStringBuilder);
            log.LogInformation($"The following user agent will be used: {UserAgent.UserAgentString}");
            Console.WriteLine("======================");
            Console.WriteLine();
        }

        private static ExplorePackagesSettings ReadSettingsFromDisk()
        {
            var settingsDirectory = Environment.GetEnvironmentVariable("USERPROFILE") ?? Directory.GetCurrentDirectory();
            var settingsPath = Path.Combine(settingsDirectory, "Knapcode.ExplorePackages.Settings.json");
            if (!File.Exists(settingsPath))
            {
                Console.WriteLine($"No settings existed at {settingsPath}.");
                return null;
            }

            Console.WriteLine($"Settings will be read from {settingsPath}.");
            var content = File.ReadAllText(settingsPath);
            var settings = JsonConvert.DeserializeObject<ExplorePackagesSettings>(content);

            return settings;
        }

        private static ServiceCollection InitializeServiceCollection(ExplorePackagesSettings settings)
        {
            var serviceCollection = new ServiceCollection();

            serviceCollection.AddExplorePackages(settings);

            serviceCollection.AddSingleton<ILogger, ConsoleLogger>();

            serviceCollection.AddTransient<PackageQueriesCommand>();
            serviceCollection.AddTransient<FetchCursorsCommand>();
            serviceCollection.AddTransient<CatalogToDatabaseCommand>();
            serviceCollection.AddTransient<CatalogToNuspecsCommand>();
            serviceCollection.AddTransient<ShowQueryResultsCommand>();
            serviceCollection.AddTransient<ShowRepositoriesCommand>();
            serviceCollection.AddTransient<CheckPackageCommand>();

            return serviceCollection;
        }
    }
}
