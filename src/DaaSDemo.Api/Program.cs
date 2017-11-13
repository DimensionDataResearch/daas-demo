﻿using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.UserSecrets;
using Microsoft.Extensions.Logging;

namespace DaaSDemo.Api
{
    /// <summary>
    ///     The Database-as-a-Service demo API.
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
        public static IWebHost BuildWebHost(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.LiterateConsole()
                .WriteTo.Debug()
                .Enrich.FromLogContext()
                .CreateLogger();

            return WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddSerilog(Log.Logger);
                })
                .ConfigureAppConfiguration((context, config) =>
                {
                    config.AddUserSecrets<Startup>();
                })
                .Build();
        }
    }
}
