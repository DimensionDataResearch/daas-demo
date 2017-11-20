using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Converters;

namespace DaaSDemo.Api
{
    using Common.Options;
    using Data;
    using Serilog;

    /// <summary>
    ///     Startup logic for the Database-as-a-Service demo API.
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
            CorsOptions = CorsOptions.From(Configuration);
            DatabaseOptions = DatabaseOptions.From(Configuration);
        }

        /// <summary>
        ///     The application configuration.
        /// </summary>
        public IConfiguration Configuration { get; }

        /// <summary>
        ///     CORS options for the application.
        /// </summary>
        public CorsOptions CorsOptions { get; }

        /// <summary>
        ///     Database options for the application.
        /// </summary>
        public DatabaseOptions DatabaseOptions { get; }

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

            services.AddEntityFrameworkSqlServer()
                .AddDbContext<Entities>(entities =>
                {
                    string connectionString = DatabaseOptions.ConnectionString;
                    if (String.IsNullOrWhiteSpace(connectionString))
                        throw new InvalidOperationException("Application configuration is missing database connection string.");

                    entities.UseSqlServer(connectionString, sqlServer =>
                    {
                        sqlServer.MigrationsAssembly("DaaSDemo.Api");
                    });
                });

            services.AddCors(cors =>
            {
                if (String.IsNullOrWhiteSpace(CorsOptions.UI))
                    throw new InvalidOperationException("Application configuration is missing CORS base address for UI.");

                // Allow requests from the UI.                
                string[] uiBaseAddresses = CorsOptions.UI.Split(';');
                Log.Information("CORS enabled for UI: {@BaseAddresses}", uiBaseAddresses);

                cors.AddPolicy("UI", policy =>
                    policy.WithOrigins(uiBaseAddresses)
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                );
            });

            services.AddMvc()
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
        }

        /// <summary>
        ///     Configure the application pipeline.
        /// </summary>
        /// <param name="app">
        ///     The application pipeline builder.
        /// </param>
        public void Configure(IApplicationBuilder app, IApplicationLifetime appLifetime)
        {
            app.UseDeveloperExceptionPage();
            app.UseCors("UI");
            app.UseMvc();

            appLifetime.ApplicationStopped.Register(Serilog.Log.CloseAndFlush);
        }
    }
}
