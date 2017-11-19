using Akka.Actor;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;

namespace DaaSDemo.Provisioning
{
    using Common.Options;

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
        public ProvisioningEngine(IOptions<DatabaseOptions> databaseOptions, IOptions<SqlExecutorClientOptions> sqlClientOptions, IOptions<KubernetesOptions> kubeOptions, IOptions<PrometheusOptions> prometheusOptions, IOptions<ProvisioningOptions> provisioningOptions)
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
            
            DatabaseOptions = databaseOptions.Value;
            SqlClientOptions = sqlClientOptions.Value;
            KubeOptions = kubeOptions.Value;
            PrometheusOptions = prometheusOptions.Value;
            ProvisioningOptions = provisioningOptions.Value;
        }

        DatabaseOptions DatabaseOptions { get; set; }
        SqlExecutorClientOptions SqlClientOptions { get; set; }
        KubernetesOptions KubeOptions { get; set; }
        PrometheusOptions PrometheusOptions { get; set; }
        ProvisioningOptions ProvisioningOptions { get; set; }

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
            if (_actorSystem != null)
                throw new InvalidOperationException("Cannot start the provisioning engine because it is already running.");

            _actorSystem = Boot.Up(DatabaseOptions, SqlClientOptions, KubeOptions, PrometheusOptions, ProvisioningOptions);

            DataAccess = _actorSystem.ActorOf(
                Props.Create<Actors.DataAccess>(),
                name: Actors.DataAccess.ActorName
            );
        }

        /// <summary>
        ///     Asynchronously stop the provisioning engine.
        /// </summary>
        public async Task Stop()
        {
            if (_actorSystem == null)
                return;

            await _actorSystem.Terminate();
        }

        /// <summary>
        ///     The <see cref="Actors.DataAccess"/> actor.
        /// </summary>
        public IActorRef DataAccess { get; private set; }
    }
}
