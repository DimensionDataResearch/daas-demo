﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Converters;

namespace DaaSDemo.DatabaseProxy
{
    using Common.Options;
    using Data;
    using Filters;

    /// <summary>
    ///     Startup logic for the Database-as-a-Service demo T-SQL proxy API.
    /// </summary>
    public class Startup
    {
        /// <summary>
        ///     Create a new <see cref="Startup"/>.
        /// </summary>
        /// <param name="configuration">
        ///     The application configuration.
        /// </param>
        public Startup(IConfiguration configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));
    
            Configuration = configuration;
            DatabaseOptions = DatabaseOptions.From(Configuration);
            KubernetesOptions = KubernetesOptions.From(Configuration);
        }

        /// <summary>
        ///     The application configuration.
        /// </summary>
        public IConfiguration Configuration { get; }

        /// <summary>
        ///     Database options for the application.
        /// </summary>
        public DatabaseOptions DatabaseOptions { get; }

        /// <summary>
        ///     Database options for the application.
        /// </summary>
        public KubernetesOptions KubernetesOptions { get; }

        /// <summary>
        ///     Configure application services.
        /// </summary>
        /// <param name="services">
        ///     The application service collection.
        /// </param>
        public void ConfigureServices(IServiceCollection services)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            services.AddOptions();
            services.AddDaaSOptions(Configuration);

            services.AddDaaSDataAccess();

            services
                .AddMvc(mvc =>
                {
                    mvc.Filters.Add(
                        new RespondWithFilter()
                    );
                })
                .AddJsonOptions(json =>
                {
                    json.SerializerSettings.Converters.Add(
                        new StringEnumConverter()
                    );
                });
            services.AddDataProtection(dataProtection =>
            {
                dataProtection.ApplicationDiscriminator = "DaaS.Demo";
            });

            if (Environment.GetEnvironmentVariable("IN_KUBERNETES") == "1")
            {
                // When running inside Kubernetes, use pod-level service account (e.g. access token from mounted Secret).
                services.AddSingleton<KubeClient.KubeApiClient>(
                    serviceProvider => KubeClient.KubeApiClient.CreateFromPodServiceAccount()
                );
            }
            else
            {
                if (String.IsNullOrWhiteSpace(KubernetesOptions.ApiEndPoint))
                    throw new InvalidOperationException("Application configuration is missing Kubernetes API end-point.");

                if (String.IsNullOrWhiteSpace(KubernetesOptions.Token))
                    throw new InvalidOperationException("Application configuration is missing Kubernetes API token.");

                // For debugging purposes only.
                services.AddSingleton<KubeClient.KubeApiClient>(
                    serviceProvider => KubeClient.KubeApiClient.Create(
                        endPointUri: new Uri(KubernetesOptions.ApiEndPoint),
                        accessToken: KubernetesOptions.Token
                    )
                );
            }

            services.AddSingleton<HttpClient>();
        }

        /// <summary>
        ///     Configure the application pipeline.
        /// </summary>
        /// <param name="app">
        ///     The application pipeline builder.
        /// </param>
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseDeveloperExceptionPage();
            app.UseMvc();
        }
    }
}
