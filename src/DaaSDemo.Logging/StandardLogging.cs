using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Sinks.Elasticsearch;
using System;
using Serilog.Events;

namespace DaaSDemo.Logging
{
    /// <summary>
    ///     Standard logging configuration.
    /// /// </summary>
    public static class StandardLogging
    {
        /// <summary>
        ///     Is the current process running inside Kubernetes?
        /// </summary>
        public static bool IsKubernetes => Environment.GetEnvironmentVariable("IN_KUBERNETES") == "1";

        /// <summary>
        ///     Configure Serilog for standard logging behaviour.
        /// </summary>
        /// <param name="configuration">
        ///     The application configuration.
        /// </param>
        /// <param name="daasComponentName">
        ///     The DaaS component name attached to log entries.
        /// </param>
        /// <returns>
        ///     The configured Serilog <see cref="ILogger"/>.
        /// </returns>
        public static ILogger ConfigureSerilog(IConfiguration configuration, string daasComponentName)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            if (String.IsNullOrWhiteSpace(daasComponentName))
                throw new ArgumentException("Argument cannot be null, empty, or entirely composed of whitespace: 'daasComponentName'.", nameof(daasComponentName));

            string logLevelValue = configuration.GetValue<string>("Logging:Level") ?? LogEventLevel.Information.ToString();
            LogEventLevel logLevel = (LogEventLevel)Enum.Parse(
                enumType: typeof(LogEventLevel),
                value: logLevelValue,
                ignoreCase: true
            );

            var loggerConfiguration = new LoggerConfiguration()
                .MinimumLevel.Is(logLevel)
                .WriteTo.LiterateConsole()
                .WriteTo.Debug()
                .Enrich.FromLogContext()
                .Enrich.WithProperty("$DaaSComponent", daasComponentName)
                .Enrich.WithDemystifiedStackTraces();

            if (IsKubernetes)
            {
                string currentPodName = configuration.GetCurrentPodName();
                if (!String.IsNullOrWhiteSpace(currentPodName))
                    loggerConfiguration.Enrich.WithProperty("$PodName", currentPodName);

                Uri elasticSearchEndPointUri = configuration.GetElasticSearchEndPoint();
                if (elasticSearchEndPointUri != null)
                {
                    loggerConfiguration.WriteTo.Elasticsearch(
                        new ElasticsearchSinkOptions(elasticSearchEndPointUri)
                        {
                            AutoRegisterTemplate = true,
                            IndexFormat = "daas-{0:yyyy.MM.dd}"
                        }
                    );
                }
            }

            return loggerConfiguration.CreateLogger();
        }

        /// <summary>
        ///     Get the name of the current Kubernetes Pod (if configured).
        /// </summary>
        /// <param name="configuration">
        ///     The application configuration.
        /// </param>
        /// <returns>
        ///     The Pod name, or <c>null</c> if no Pod name is configured.
        /// </returns>
        static string GetCurrentPodName(this IConfiguration configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            return configuration.GetValue<string>("Kubernetes:PodName");
        }

        /// <summary>
        ///     Get the end-point (if configured) for ElasticSearch.
        /// </summary>
        /// <param name="configuration">
        ///     The application configuration.
        /// </param>
        /// <returns>
        ///     The end-point URI, or <c>null</c> if no end-point is configured.
        /// </returns>
        static Uri GetElasticSearchEndPoint(this IConfiguration configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            string endPoint = configuration.GetValue<string>("Logging:ElasticSearch:EndPoint");
            if (String.IsNullOrWhiteSpace(endPoint))
                return null;

            return new Uri(endPoint);
        }
    }
}
