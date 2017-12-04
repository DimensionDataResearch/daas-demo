using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DaaSDemo.UI
{
    using Logging;

    /// <summary>
    ///     The Database-as-a-Service demo UI.
    /// </summary>
    public static class Program
    {
        /// <summary>
        ///     The main program entry point.
        /// </summary>
        /// <param name="args">
        ///     Command-line arguments.
        /// </param>
        public static void Main(string[] args) => BuildWebHost(args).Run();

        /// <summary>
        ///     Create the application web host.
        /// </summary>
        /// <param name="args">
        ///     Command-line arguments.
        /// </param>
        /// <returns>
        ///     The <see cref="IWebHost"/>.
        /// </returns>
        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .UseUrls("http://localhost:5000")
                .ConfigureAppConfiguration((context, config) =>
                {
                    config.AddJsonFile("appsettings.json");
                    config.AddUserSecrets<Startup>();
                    config.AddEnvironmentVariables(prefix: "DAAS_");
                })
                .ConfigureLogging((context, logging) =>
                {
                    Log.Logger = StandardLogging.ConfigureSerilog(context.Configuration,
                        daasComponentName: "UI"
                    );

                    logging.ClearProviders();
                    logging.AddSerilog(Log.Logger);
                })
                .Build();
    }
}
