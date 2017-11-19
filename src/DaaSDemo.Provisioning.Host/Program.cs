using Microsoft.EntityFrameworkCore;
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
                    services.AddOptions()
                        .Configure<DatabaseOptions>("Database", hostContext.Configuration)
                        .Configure<SqlExecutorClientOptions>("SQL", hostContext.Configuration)
                        .Configure<KubernetesOptions>("Kubernetes", hostContext.Configuration)
                        .Configure<PrometheusOptions>("Prometheus", hostContext.Configuration)
                        .Configure<ProvisioningOptions>("Provisioning", hostContext.Configuration);

                    DatabaseOptions databaseOptions = DatabaseOptions.From(hostContext.Configuration);
                    if (String.IsNullOrWhiteSpace(databaseOptions.ConnectionString))
                        throw new InvalidOperationException("Application configuration is missing database connection string.");

                    services.AddEntityFrameworkSqlServer()
                        .AddDbContext<Entities>(entities =>
                        {
                            entities.UseSqlServer(databaseOptions.ConnectionString);
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
                            KubernetesOptions kubernetesOptions = serviceProvider.GetRequiredService<IOptions<KubernetesOptions>>().Value;
                            if (String.IsNullOrWhiteSpace(kubernetesOptions.ApiEndPoint))
                                throw new InvalidOperationException("Application configuration is missing Kubernetes API end-point.");

                            return KubeClient.KubeApiClient.Create(
                                endPointUri: new Uri(kubernetesOptions.ApiEndPoint),
                                accessToken: kubernetesOptions.Token
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
