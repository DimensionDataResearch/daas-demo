using Akka;
using Akka.Actor;
using Akka.Configuration;
using Microsoft.Extensions.Configuration;
using System;

namespace DaaSDemo.Provisioning
{
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
        /// <param name="appConfiguration">
        ///     The application-level configuration.
        /// </param>
        public static ActorSystem Up(IConfiguration appConfiguration)
        {
            string connectionString = appConfiguration["Database:ConnectionString"];
            Config databaseConfig = ConfigurationFactory.ParseString($@"
                daas.db.connection-string = ""{connectionString}""
            ");

            string kubeApiEndpoint = appConfiguration["Kubernetes:ApiEndPoint"];
            string kubeApiToken = appConfiguration["Kubernetes:Token"];
            string volumeClaimName = appConfiguration["Kubernetes:VolumeClaimName"];
            Config kubeConfig = ConfigurationFactory.ParseString($@"
                daas.kube.api-endpoint = ""{kubeApiEndpoint}""
                daas.kube.api-token = ""{kubeApiToken}""
                daas.kube.volume-claim-name = ""{volumeClaimName}""
            ");

            string sqlApiEndPoint = appConfiguration["SQL:ApiEndPoint"];
            Config sqlConfig = ConfigurationFactory.ParseString($@"
                daas.sql.api-endpoint = ""{sqlApiEndPoint}""
            ");

            return ActorSystem.Create("daas-demo",
                BaseConfiguration
                    .WithFallback(databaseConfig)
                    .WithFallback(kubeConfig)
                    .WithFallback(sqlConfig)
            );
        }
    }
}
