﻿using Microsoft.Extensions.Logging;

namespace Knapcode.ExplorePackages.Support
{
    public static class MinimalConsoleLoggerFactoryExtensions
    {
        public static ILoggerFactory AddMinimalConsole(this ILoggerFactory factory)
        {
            factory.AddProvider(new MinimalConsoleLoggerProvider());
            return factory;
        }
    }
}