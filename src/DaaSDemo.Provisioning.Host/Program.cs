using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using System.Threading.Tasks;
using System;
using System.IO;

namespace DaaSDemo.Provisioning.Host
{
    using Data;

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
                .ConfigureLogging(logging =>
                {
                    Log.Logger = new LoggerConfiguration()
                        .MinimumLevel.Information()
                        .WriteTo.LiterateConsole()
                        .WriteTo.Debug()
                        .Enrich.FromLogContext()
                        .Enrich.WithDemystifiedStackTraces()
                        .CreateLogger();

                    logging.ClearProviders();
                    logging.AddSerilog(Log.Logger);
                })
                .ConfigureAppConfiguration((context, config) =>
                {
                    config.AddJsonFile("appsettings.json");
                    config.AddUserSecrets(
                        typeof(Program).Assembly
                    );
                    config.AddEnvironmentVariables(prefix: "DAAS_");
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddEntityFrameworkSqlServer()
                        .AddDbContext<Entities>(entities =>
                        {
                            entities.UseSqlServer(
                                connectionString: hostContext.Configuration.GetValue<string>("Database:ConnectionString")
                            );
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
                        // For debugging purposes only.
                        services.AddTransient<KubeClient.KubeApiClient>(serviceProvider =>
                        {
                            return KubeClient.KubeApiClient.Create(
                                endPointUri: new Uri(
                                    hostContext.Configuration.GetValue<string>("Kubernetes:ApiEndPoint")
                                ),
                                accessToken: hostContext.Configuration.GetValue<string>("Kubernetes:Token")
                            );
                        });
                    }

                    services.AddSingleton<ProvisioningEngine>();
                    services.AddScoped<IHostedService, ProvisioningService>();
                })
                .RunConsoleAsync();
        }
    }
}
