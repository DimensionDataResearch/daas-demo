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

            string apiEndpoint = appConfiguration["Kubernetes:ApiEndPoint"];
            string apiToken = appConfiguration["Kubernetes:Token"];
            string volumeClaimName = appConfiguration["Kubernetes:VolumeClaimName"];
            Config kubeConfig = ConfigurationFactory.ParseString($@"
                daas.kube.api-endpoint = ""{apiEndpoint}""
                daas.kube.api-token = ""{apiToken}""
                daas.kube.volume-claim-name = ""{volumeClaimName}""
            ");

            return ActorSystem.Create("daas-demo",
                BaseConfiguration
                    .WithFallback(databaseConfig)
                    .WithFallback(kubeConfig)
            );
        }
    }
}
