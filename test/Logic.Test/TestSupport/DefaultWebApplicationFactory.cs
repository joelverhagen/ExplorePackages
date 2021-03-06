// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace NuGet.Insights
{
    public class DefaultWebApplicationFactory<TStartup>
        : WebApplicationFactory<TStartup> where TStartup : class
    {
        protected override IWebHostBuilder CreateWebHostBuilder()
        {
            return WebHost
                .CreateDefaultBuilder()
                .ConfigureAppConfiguration(x =>
                {
                    // Source: https://github.com/dotnet/runtime/issues/27272#issuecomment-497515971
                    x.Sources.Clear();
                })
                .UseStartup<TStartup>();
        }
    }
}
