using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using System.Threading.Tasks;
using System;
using System.IO;

namespace DaaSDemo.Provisioning.Host
{
    using Common.Options;
    using Data;
    using KubeClient;
    using Logging;
    using DatabaseProxy.Client;

    /// <summary>
    ///     Host for the Database-as-a-Service provisioning engine.
    /// </summary>
    static class Program
    {
        /// <summary>
        ///     The main program entry-point.
        /// </summary>
        /// <param name="args">
        ///     Command-line arguments.
        /// </param>
        static async Task Main(string[] args)
        {
            await new HostBuilder()
                .ConfigureAppConfiguration((context, config) =>
                {
                    config.AddJsonFile("appsettings.json");
                    config.AddUserSecrets(
                        typeof(Program).Assembly
                    );
                    config.AddEnvironmentVariables(prefix: "DAAS_");
                })
                .ConfigureLogging((context, logging) =>
                {
                    Log.Logger = StandardLogging.ConfigureSerilog(context.Configuration,
                        daasComponentName: "Provisioning"
                    );

                    logging.ClearProviders();
                    logging.AddSerilog(Log.Logger);
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddOptions();
                    services.AddDaaSOptions(hostContext.Configuration);

                    services.AddDaaSDataAccess();

                    services.AddKubeClient();
                    services.AddDatabaseProxyApiClient();
                    services.AddProvisioning();

                    DatabaseOptions databaseOptions = DatabaseOptions.From(hostContext.Configuration);
                    if (String.IsNullOrWhiteSpace(databaseOptions.ConnectionString))
                        throw new InvalidOperationException("Application configuration is missing database connection string.");
                    
                    services.AddScoped<IHostedService, ProvisioningService>();
                })
                .RunConsoleAsync();
        }
    }
}
