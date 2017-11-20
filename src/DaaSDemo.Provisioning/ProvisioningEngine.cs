using Akka.Actor;
using Akka.DI.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
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
        ///     A factory for actor-level dependency injection scopes.
        /// </summary>
        readonly IServiceScopeFactory _scopeFactory;

        /// <summary>
        ///     The underlying actor system.
        /// </summary>
        ActorSystem _actorSystem;

        /// <summary>
        ///     Create a new <see cref="ProvisioningEngine"/>.
        /// </summary>
        /// <param name="scopeFactory">
        ///     A factory for actor-level dependency injection scopes.
        /// </param>
        /// <param name="databaseOptions">
        ///     The application-level database options.
        /// </param>
        /// <param name="sqlClientOptions">
        ///     The application-level SQL Executor API client options.
        /// </param>
        /// <param name="kubeOptions">
        ///     The application-level Kubernetes options.
        /// </param>
        /// <param name="prometheusOptions">
        ///     The application-level Prometheus options.
        /// </param>
        /// <param name="provisioningOptions">
        ///     The application-level provisioning options.
        /// </param>
        public ProvisioningEngine(IServiceScopeFactory scopeFactory, IOptions<DatabaseOptions> databaseOptions, IOptions<SqlExecutorClientOptions> sqlClientOptions, IOptions<KubernetesOptions> kubeOptions, IOptions<PrometheusOptions> prometheusOptions, IOptions<ProvisioningOptions> provisioningOptions)
        {
            if (scopeFactory == null)
                throw new ArgumentNullException(nameof(scopeFactory));

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
            
            _scopeFactory = scopeFactory;
            DatabaseOptions = databaseOptions.Value;
            SqlClientOptions = sqlClientOptions.Value;
            KubeOptions = kubeOptions.Value;
            PrometheusOptions = prometheusOptions.Value;
            ProvisioningOptions = provisioningOptions.Value;
        }

        /// <summary>
        ///     The application-level database options.
        /// </summary>
        DatabaseOptions DatabaseOptions { get; set; }

        /// <summary>
        ///     The application-level SQL Executor API client options.
        /// </summary>
        SqlExecutorClientOptions SqlClientOptions { get; set; }

        /// <summary>
        ///     The application-level Kubernetes options.
        /// </summary>
        KubernetesOptions KubeOptions { get; set; }

        /// <summary>
        ///     The application-level Prometheus options.
        /// </summary>
        PrometheusOptions PrometheusOptions { get; set; }

        /// <summary>
        ///     The application-level provisioning options.
        /// </summary>
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

            _actorSystem = Boot.Up(_scopeFactory);

            DataAccess = _actorSystem.ActorOf(
                _actorSystem.DI().Props<Actors.DataAccess>(),
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
