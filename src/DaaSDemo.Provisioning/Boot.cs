using Akka;
using Akka.Actor;
using Akka.Configuration;
using Microsoft.Extensions.Configuration;
using System;

namespace DaaSDemo.Provisioning
{
    using Common.Options;

    /// <summary>
    ///     Startup logic for the provisioning actor system.
    /// </summary>
    public static class Boot
    {
        /// <summary>
        ///     The actor system configuration.
        /// </summary>
        public static Config BaseConfiguration { get; } = ConfigurationFactory.FromResource(
            resourceName: "DaaSDemo.Provisioning.ActorSystem.conf",
            assembly: typeof(Boot).Assembly
        );

        /// <summary>
        ///     Start the provisioning actor system.
        /// </summary>
        /// <param name="databaseOptions">
        ///     The application's database options.
        /// </param>
        /// <param name="sqlClientOptions">
        ///     The application's SQL Executor API client options.
        /// </param>
        /// <param name="kubeOptions">
        ///     The application's Kubernetes options.
        /// </param>
        /// <param name="prometheusOptions">
        ///     The application's Prometheus options.
        /// </param>
        /// <param name="provisioningOptions">
        ///     The application's provisioning options.
        /// </param>
        /// <returns>
        ///     The configured <see cref="ActorSystem"/>.
        /// </returns>
        public static ActorSystem Up(DatabaseOptions databaseOptions, SqlExecutorClientOptions sqlClientOptions, KubernetesOptions kubeOptions, PrometheusOptions prometheusOptions, ProvisioningOptions provisioningOptions)
        {
            if (databaseOptions == null)
                throw new ArgumentNullException(nameof(databaseOptions));

            if (sqlClientOptions == null)
                throw new ArgumentNullException(nameof(sqlClientOptions));

            if (kubeOptions == null)
                throw new ArgumentNullException(nameof(kubeOptions));

            if (prometheusOptions == null)
                throw new ArgumentNullException(nameof(prometheusOptions));

            if (provisioningOptions == null)
                throw new ArgumentNullException(nameof(provisioningOptions));

            Config databaseConfig = ConfigurationFactory.ParseString($@"
                daas.db.connection-string = ""{databaseOptions.ConnectionString}""
            ");

            Config provisioningConfig = ConfigurationFactory.ParseString($@"
                daas.kube.sql-image-name = ""{provisioningOptions.Images.SQL}""
                daas.kube.sql-exporter-image-name = ""{provisioningOptions.Images.SQLExporter}""
            ");

            Config kubeConfig = ConfigurationFactory.ParseString($@"
                daas.kube.api-endpoint = ""{kubeOptions.ApiEndPoint}""
                daas.kube.api-token = ""{kubeOptions.Token}""
                daas.kube.cluster-public-fqdn = ""{kubeOptions.ClusterPublicFQDN}""
                daas.kube.volume-claim-name = ""{kubeOptions.VolumeClaimName}""
            ");

            Config prometheusConfig = ConfigurationFactory.ParseString($@"
                daas.prometheus.enable = {prometheusOptions.Enable}
                daas.prometheus.api-endpoint = ""{prometheusOptions.ApiEndPoint}""
            ");

            Config sqlClientConfig = ConfigurationFactory.ParseString($@"
                daas.sql.api-endpoint = ""{sqlClientOptions.ApiEndPoint}""
            ");

            return ActorSystem.Create("daas-demo",
                BaseConfiguration
                    .WithFallback(databaseConfig)
                    .WithFallback(provisioningConfig)
                    .WithFallback(prometheusConfig)
                    .WithFallback(kubeConfig)
                    .WithFallback(sqlClientConfig)
            );
        }
    }
}
