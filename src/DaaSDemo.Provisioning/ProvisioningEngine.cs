using Akka.Actor;
using System;
using Microsoft.Extensions.Configuration;

namespace DaaSDemo.Provisioning
{
    /// <summary>
    ///     The provisioning engine API.
    /// </summary>
    public class ProvisioningEngine
        : IDisposable
    {
        /// <summary>
        ///     The application-level configuration.
        /// </summary>
        readonly IConfiguration _configuration;

        /// <summary>
        ///     The underlying actor system.
        /// </summary>
        ActorSystem _actorSystem;

        /// <summary>
        ///     Create a new <see cref="ProvisioningEngine"/>.
        /// </summary>
        /// <param name="configuration">
        ///     The application-level configuration.
        /// </param>
        public ProvisioningEngine(IConfiguration configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));
            
            _configuration = configuration;
        }

        /// <summary>
        ///     Dispose of resources being used by the provisioning engine.
        /// </summary>
        public void Dispose()
        {
            _actorSystem.Dispose();
        }

        /// <summary>
        ///     Start the provisioning engine.
        /// </summary>
        public void Start()
        {
            _actorSystem = Boot.Up(_configuration);

            DatabaseWatcher = _actorSystem.ActorOf(
                Props.Create<Actors.DataAccess>(),
                name: Actors.DataAccess.ActorName
            );
        }

        /// <summary>
        ///     The <see cref="Actors.DataAccess"/> actor.
        /// </summary>
        public IActorRef DatabaseWatcher { get; private set; }
    }
}