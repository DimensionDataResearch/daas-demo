using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Raven.Client.Documents;
using Raven.Client.Documents.Indexes;
using Serilog;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DaaSDemo.Tools.CreateIndexes
{
    using Common.Options;
    using Data;
    using Data.Indexes;

    /// <summary>
    ///     Tool to create / update indexes in the DaaS management database.
    /// </summary>
    static class Program
    {
        /// <summary>
        ///     The main program entry-point.
        /// </summary>
        /// <returns>
        ///     A <see cref="Task"/> representing program execution.
        /// </returns>
        static async Task Main(string[] args)
        {
            ConfigureLogging();
            
            try
            {
                Log.Information("Creating indexes...");
                
                IServiceProvider serviceProvider = BuildServiceProvider();
                IDocumentStore documentStore = serviceProvider.GetRequiredService<IDocumentStore>();

                await IndexCreation.CreateIndexesAsync(
                    typeof(AppUserDetails).Assembly,
                    documentStore
                );

                Log.Information("Done...");
            }
            catch (Exception createInitialDataFailed)
            {
                Log.Error(createInitialDataFailed, "Failed to create initial data. {ErrorMessage}", createInitialDataFailed.Message);
            }
        }

        /// <summary>
        ///     Build a service provider for use in the test harness.
        /// </summary>
        /// <returns>
        ///     The service provider.
        /// </returns>
        static IServiceProvider BuildServiceProvider()
        {
            IConfiguration configuration = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .AddUserSecrets(
                    typeof(Program).Assembly
                )
                .Build();

            IServiceCollection services = new ServiceCollection();

            services.AddDaaSOptions(configuration);
            services.AddDaaSDataAccess();

            services.AddLogging(logging =>
            {
                logging.AddSerilog(Log.Logger);
            });

            services.AddDataProtection(dataProtection =>
            {
                dataProtection.ApplicationDiscriminator = "DaaS.Demo";
            });

            return services.BuildServiceProvider();
        }

        /// <summary>
        ///     Configure the global logger.
        /// </summary>
        static void ConfigureLogging()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .Enrich.FromLogContext()
                .WriteTo.Debug()
                .WriteTo.LiterateConsole()
                .CreateLogger();
        }
    }
}
