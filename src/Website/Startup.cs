﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using NuGet.Insights.Worker;

namespace NuGet.Insights.Website
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<NuGetInsightsSettings>(Configuration.GetSection(NuGetInsightsSettings.DefaultSectionName));
            services.Configure<NuGetInsightsWorkerSettings>(Configuration.GetSection(NuGetInsightsSettings.DefaultSectionName));
            services.Configure<NuGetInsightsWebsiteSettings>(Configuration.GetSection(NuGetInsightsSettings.DefaultSectionName));

            services.AddNuGetInsights("NuGet.Insights.Website");
            services.AddNuGetInsightsWorker();

            services.AddScoped<IAuthorizationHandler, AllowListAuthorizationHandler>();
            services.AddScoped<AllowListAuthorizationHandler>();

            services.AddApplicationInsightsTelemetry(options =>
            {
                options.InstrumentationKey = Configuration["APPINSIGHTS_INSTRUMENTATIONKEY"];
                options.EnableAdaptiveSampling = false;
            });

            services
                .AddMvc();

            var microsoftIdentityBuilder = services
                .AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
                .AddMicrosoftIdentityWebApp(options =>
                {
                    Configuration.GetSection(Constants.AzureAd).Bind(options);
                    options.Events.OnTokenValidated = async context =>
                    {
                        await context
                            .HttpContext
                            .RequestServices
                            .GetRequiredService<AllowListAuthorizationHandler>()
                            .AddAllowedGroupClaimsAsync(context);
                    };
                },
                options =>
                {
                    options.ExpireTimeSpan = TimeSpan.FromHours(6);
                    options.SlidingExpiration = true;
                    options.AccessDeniedPath = "/Home/AccessDenied";
                });

            var initialSettings = Configuration
                .GetSection(NuGetInsightsSettings.DefaultSectionName)
                .Get<NuGetInsightsWebsiteSettings>();
            if (initialSettings?.AllowedGroups is not null && initialSettings.AllowedGroups.Any())
            {
                microsoftIdentityBuilder
                    .EnableTokenAcquisitionToCallDownstreamApi(new[] { "user.read" })
                    .AddInMemoryTokenCaches()
                    .AddMicrosoftGraph();
            }

            services
                .AddAuthorization(options =>
                {
                    options.AddPolicy(
                        AllowListAuthorizationHandler.PolicyName,
                        policy => policy.Requirements.Add(new AllowListRequirement()));
                });

            services
                .AddRazorPages()
                .AddMicrosoftIdentityUI();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();

            app.UseRouting();

            app.UseHsts();
            app.Use(async (context, next) =>
            {
                context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
                await next();
            });

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(routes =>
            {
                routes.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}");
            });
        }
    }
}
