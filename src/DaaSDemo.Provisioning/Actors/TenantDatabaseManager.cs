using Akka;
using Akka.Actor;
using HTTPlease;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace DaaSDemo.Provisioning.Actors
{
    using Data;
    using DatabaseProxy.Client;
    using Exceptions;
    using Messages;
    using Models.Data;
    using Models.DatabaseProxy;
    using Provisioners;

    /// <summary>
    ///     Actor that manages a specific tenant database.
    /// </summary>
    public class TenantDatabaseManager
        : ReceiveActorEx
    {
        /// <summary>
        ///     Create a new <see cref="TenantDatabaseManager"/>.
        /// </summary>
        /// <param name="registeredProvisioners">
        ///     All registered database provisioners.
        /// </param>
        /// <param name="databaseProxyClient">
        ///     The <see cref="DatabaseProxyApiClient"/> used to communicate with the Database Proxy API.
        /// </param>
        public TenantDatabaseManager(IEnumerable<DatabaseProvisioner> registeredProvisioners, DatabaseProxyApiClient databaseProxyClient)
        {
            if (registeredProvisioners == null)
                throw new ArgumentNullException(nameof(registeredProvisioners));

            if (databaseProxyClient == null)
                throw new ArgumentNullException(nameof(databaseProxyClient));

            RegisteredProvisioners = registeredProvisioners;
            DatabaseProxyClient = databaseProxyClient;
        }

        /// <summary>
        ///     All registered database provisioners.
        /// </summary>
        IEnumerable<DatabaseProvisioner> RegisteredProvisioners { get; }

        /// <summary>
        ///     The <see cref="DatabaseProxyApiClient"/> used to communicate with the Database proxy API.
        /// </summary>
        DatabaseProxyApiClient DatabaseProxyClient { get; set; }

        /// <summary>
        ///     The active database provisioner.
        /// </summary>
        DatabaseProvisioner ActiveProvisioner { get; set; }

        /// <summary>
        ///     A reference to the <see cref="Actors.TenantServerManager"/> actor whose server hosts the database.
        /// </summary>
        IActorRef ServerManager { get; set; }

        /// <summary>
        ///     A reference to the <see cref="Actors.DataAccess"/> actor.
        /// </summary>
        IActorRef DataAccess { get; set; }

        /// <summary>
        ///     A <see cref="DatabaseServer"/> representing the server that hosts the database.
        /// </summary>
        DatabaseServer Server { get; set; }

        /// <summary>
        ///     Called when the actor is started.
        /// </summary>
        protected override void PreStart()
        {
            Become(Initializing);
        }
        
        /// <summary>
        ///     Called when the actor is stopped.
        /// </summary>
        protected override void PostStop()
        {
            if (DatabaseProxyClient != null)
            {
                DatabaseProxyClient.Dispose();
                DatabaseProxyClient = null;
            }
        }

        /// <summary>
        ///     Called when the actor is initialising.
        /// </summary>
        void Initializing()
        {
            Receive<Initialize>(initialize =>
            {
                ServerManager = initialize.ServerManager;
                DataAccess = initialize.DataAccess;
                Server = initialize.Server;

                ActiveProvisioner = RegisteredProvisioners.FirstOrDefault(
                    provisioner => provisioner.SupportsServerKind(Server.Kind)
                );
                if (ActiveProvisioner == null) // If we get here it's a bug, and should bring down the entire engine.
                    throw new FatalProvisioningException($"No registered database provisioner supports servers of type {Server.Kind}.");

                ActiveProvisioner.State = initialize.InitialState;

                Self.Tell(ActiveProvisioner.State); // Kick off initial state-management actions.

                Become(Ready);
            });

            SetReceiveTimeout(
                TimeSpan.FromSeconds(5)
            );
            Receive<ReceiveTimeout>(_ =>
            {
                Log.Error("Failed to receive Initialize message within 5 seconds of being created.");

                Context.Stop(Self);
            });  
        }

        /// <summary>
        ///     Called when the actor is ready to process requests.
        /// </summary>
        void Ready()
        {
            ReceiveAsync<DatabaseInstance>(async database =>
            {
                ActiveProvisioner.State = database.Clone();

                Log.Debug("Received database configuration (Id:{DatabaseId}, Name:{DatabaseName}).",
                    ActiveProvisioner.State.Id,
                    ActiveProvisioner.State.Name
                );

                switch (database.Action)
                {
                    case ProvisioningAction.Provision:
                    {
                        await Provision();

                        break;
                    }
                    case ProvisioningAction.Deprovision:
                    {
                        if (await Deprovision())
                            Context.Stop(Self);

                        break;
                    }
                }
            });
        }

        /// <summary>
        ///     Provision the database.
        /// </summary>
        /// <returns>
        ///     A <see cref="Task"/> representing the operation.
        /// </returns>
        async Task Provision()
        {
            Log.Info("Provisioning database {DatabaseId} in server {ServerId}...",
                ActiveProvisioner.State.Id,
                ActiveProvisioner.State.ServerId
            );

            DataAccess.Tell(
                new DatabaseProvisioning(ActiveProvisioner.State.Id)
            );

            try
            {
                if (await ActiveProvisioner.DoesDatabaseExist())
                {
                    Log.Info("Database {DatabaseName} already exists; will treat as provisioned.",
                        ActiveProvisioner.State.Id,
                        ActiveProvisioner.State.Name
                    );
                }
                else
                    await ActiveProvisioner.CreateDatabase();

                DataAccess.Tell(
                    new DatabaseProvisioned(ActiveProvisioner.State.Id)
                );
            }
            catch (ProvisioningException createDatabaseFailed)
            {
                Log.Error(createDatabaseFailed, "Unexpected error creating database {DatabaseName} (Id:{DatabaseId}).",
                    ActiveProvisioner.State.Name,
                    ActiveProvisioner.State.Id
                );

                DataAccess.Tell(
                    new DatabaseProvisioningFailed(ActiveProvisioner.State.Id)
                );
            }
        }

        /// <summary>
        ///     De-provision the database.
        /// </summary>
        /// <returns>
        ///     <c>true</c>, if the database was successfully de-provisioned; otherwise, <c>false<c/>.
        /// </returns>
        async Task<bool> Deprovision()
        {
            Log.Info("De-provisioning database {DatabaseId} in server {ServerId}...",
                ActiveProvisioner.State.Id,
                ActiveProvisioner.State.ServerId
            );

            DataAccess.Tell(
                new DatabaseDeprovisioning(ActiveProvisioner.State.Id)
            );

            try
            {
                if (!await ActiveProvisioner.DoesDatabaseExist())
                {
                    Log.Info("Database {DatabaseName} not found; will treat as deprovisioned.",
                        ActiveProvisioner.State.Id,
                        ActiveProvisioner.State.Name
                    );
                }
                else
                    await ActiveProvisioner.DropDatabase();

                DataAccess.Tell(
                    new DatabaseDeprovisioned(ActiveProvisioner.State.Id)
                );

                return true;
            }
            catch (ProvisioningException dropDatabaseFailed)
            {
                Log.Error(dropDatabaseFailed, "Unexpected error dropping database {DatabaseName} (Id:{DatabaseId}).",
                    ActiveProvisioner.State.Name,
                    ActiveProvisioner.State.Id
                );

                return false;
            }
        }

        /// <summary>
        ///     Initialise the actor.
        /// </summary>
        public class Initialize
        {
            /// <summary>
            ///     Create a new <see cref="Initialize"/> message.
            /// </summary>
            /// <param name="serverManager">
            ///     A reference to the <see cref="Actors.TenantServerManager"/> actor whose server hosts the database.
            /// </param>
            /// <param name="dataAccess">
            ///     A reference to the <see cref="Actors.DataAccess"/> actor.
            /// </param>
            /// <param name="server">
            ///     A <see cref="DatabaseServer"/> representing the server that hosts the database.
            /// </param>
            /// <param name="initialState">
            ///     A <see cref="DatabaseInstance"/> representing the actor's initial state.
            /// </param>
            public Initialize(IActorRef serverManager, IActorRef dataAccess, DatabaseServer server, DatabaseInstance initialState)
            {
                if (serverManager == null)
                    throw new ArgumentNullException(nameof(serverManager));
                
                if (dataAccess == null)
                    throw new ArgumentNullException(nameof(dataAccess));

                if (server == null)
                    throw new ArgumentNullException(nameof(server));
                
                if (initialState == null)
                    throw new ArgumentNullException(nameof(initialState));

                InitialState = initialState;
                Server = server;
                ServerManager = serverManager;
                DataAccess = dataAccess;
            }

            /// <summary>
            ///     The Id of the target database.
            /// </summary>
            public string DatabaseId => InitialState.Id;

            /// <summary>
            ///     A reference to the <see cref="Actors.TenantServerManager"/> actor whose server hosts the database.
            /// </summary>
            public IActorRef ServerManager { get; }

            /// <summary>
            ///     A reference to the <see cref="Actors.DataAccess"/> actor.
            /// </summary>
            public IActorRef DataAccess { get; }

            /// <summary>
            ///     A <see cref="DatabaseServer"/> representing the server that hosts the database.
            /// </summary>
            public DatabaseServer Server { get; }

            /// <summary>
            ///     A <see cref="DatabaseInstance"/> representing the actor's initial state.
            /// </summary>
            public DatabaseInstance InitialState { get; }
        }

        /// <summary>
        ///     Get the name of the <see cref="TenantServerManager"/> actor for the specified tenant.
        /// </summary>
        /// <param name="tenantId">
        ///     The database Id.
        /// </param>
        /// <returns>
        ///     The actor name.
        /// </returns>
        public static string ActorName(string databaseId) => $"database-manager.{databaseId}";
    }
}
