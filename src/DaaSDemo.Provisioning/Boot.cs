using Akka;
using Akka.Actor;
using Akka.Configuration;
using Akka.DI.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace DaaSDemo.Provisioning
{
    using Common.Options;
    using DI;

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
        /// <param name="scopeFactory">
        ///     A factory for actor-level dependency injection scopes.
        /// </param>
        /// <returns>
        ///     The configured <see cref="ActorSystem"/>.
        /// </returns>
        public static ActorSystem Up(IServiceScopeFactory scopeFactory)
        {
            ActorSystem system = ActorSystem.Create("daas-demo", BaseConfiguration);

            system.AddDependencyResolver(
                new MedDependencyResolver(system, scopeFactory)
            );

            return system;
        }
    }
}
