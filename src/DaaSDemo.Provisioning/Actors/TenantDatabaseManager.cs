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
    using Messages;
    using Models.Data;
    using Models.Sql;
    using Exceptions;
    using SqlExecutor.Client;

    /// <summary>
    ///     Actor that manages a specific tenant database.
    /// </summary>
    public class TenantDatabaseManager
        : ReceiveActorEx
    {
        /// <summary>
        ///     Create a new <see cref="TenantDatabaseManager"/>.
        /// </summary>
        /// <param name="serverManager">
        ///     A reference to the <see cref="TenantServerManager"/> actor.
        /// </param>
        /// <param name="dataAccess">
        ///     A reference to the <see cref="Actors.DataAccess"/> actor.
        /// </param>
        public TenantDatabaseManager(SqlApiClient sqlClient)
        {
            if (sqlClient == null)
                throw new ArgumentNullException(nameof(sqlClient));

            SqlClient = sqlClient;
        }

        /// <summary>
        ///     The <see cref="SqlApiClient"/> used to communicate with the SQL executor API.
        /// </summary>
        SqlApiClient SqlClient { get; set; }

        /// <summary>
        ///     A reference to the <see cref="Actors.TenantServerManager"/> actor whose server hosts the database.
        /// </summary>
        IActorRef ServerManager { get; set; }

        /// <summary>
        ///     A reference to the <see cref="Actors.DataAccess"/> actor.
        /// </summary>
        IActorRef DataAccess { get; set; }

        /// <summary>
        ///     A <see cref="DatabaseInstance"/> representing the currently-desired database state.
        /// </summary>
        DatabaseInstance CurrentState { get; set; }

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
            if (SqlClient != null)
            {
                SqlClient.Dispose();
                SqlClient = null;
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
                CurrentState = initialize.InitialState;

                Self.Tell(CurrentState); // Kick off initial state-management actions.

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
                CurrentState = database.Clone();

                Log.Debug("Received database configuration (Id:{DatabaseId}, Name:{DatabaseName}).",
                    CurrentState.Id,
                    CurrentState.Name
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
                CurrentState.Id,
                CurrentState.ServerId
            );

            DataAccess.Tell(
                new DatabaseProvisioning(CurrentState.Id)
            );

            try
            {
                if (!await DoesDatabaseExist())
                    await CreateDatabase();
                else
                {
                    Log.Info("Database {DatabaseName} already exists; will treat as provisioned.",
                        CurrentState.Id,
                        CurrentState.Name
                    );
                }

                DataAccess.Tell(
                    new DatabaseProvisioned(CurrentState.Id)
                );
            }
            catch (ProvisioningException createDatabaseFailed)
            {
                Log.Error(createDatabaseFailed, "Unexpected error creating database {DatabaseName} (Id:{DatabaseId}).",
                    CurrentState.Name,
                    CurrentState.Id
                );

                DataAccess.Tell(
                    new DatabaseProvisioningFailed(CurrentState.Id)
                );
            }
        }

        /// <summary>
        ///     De-rovision the database.
        /// </summary>
        /// <returns>
        ///     <c>true</c>, if the database was successfully de-provisioned; otherwise, <c>false<c/>.
        /// </returns>
        async Task<bool> Deprovision()
        {
            Log.Info("De-provisioning database {DatabaseId} in server {ServerId}...",
                CurrentState.Id,
                CurrentState.ServerId
            );

            DataAccess.Tell(
                new DatabaseDeprovisioning(CurrentState.Id)
            );

            try
            {
                if (await DoesDatabaseExist())
                    await DropDatabase();
                else
                {
                    Log.Info("Database {DatabaseName} not found; will treat as deprovisioned.",
                        CurrentState.Id,
                        CurrentState.Name
                    );
                }

                DataAccess.Tell(
                    new DatabaseDeprovisioned(CurrentState.Id)
                );

                return true;
            }
            catch (ProvisioningException dropDatabaseFailed)
            {
                Log.Error(dropDatabaseFailed, "Unexpected error dropping database {DatabaseName} (Id:{DatabaseId}).",
                    CurrentState.Name,
                    CurrentState.Id
                );

                return false;
            }
        }

        /// <summary>
        ///     Check if the target database exists.
        /// </summary>
        /// <returns>
        ///     <c>true</c>, if the database exists; otherwise, <c>false</c>.
        /// </returns>
        async Task<bool> DoesDatabaseExist()
        {
            QueryResult result = await SqlClient.ExecuteQuery(
                serverId: CurrentState.ServerId,
                databaseId: SqlApiClient.MasterDatabaseId,
                sql: ManagementSql.CheckDatabaseExists(),
                parameters: ManagementSql.Parameters.CheckDatabaseExists(
                    databaseName: CurrentState.Name
                ),
                executeAsAdminUser: true
            );

            return result.ResultSets[0].Rows.Count == 1;
        }

        /// <summary>
        ///     Create the database.
        /// </summary>
        /// <returns>
        ///     A <see cref="Task"/> representing the operation.
        /// </returns>
        async Task CreateDatabase()
        {
            Log.Info("Creating database {DatabaseName} (Id:{DatabaseId}) on server {ServerId}...",
                CurrentState.Name,
                CurrentState.Id,
                CurrentState.ServerId
            );

            CommandResult commandResult = await SqlClient.ExecuteCommand(
                serverId: CurrentState.ServerId,
                databaseId: SqlApiClient.MasterDatabaseId,
                sql: ManagementSql.CreateDatabase(CurrentState.Name, CurrentState.DatabaseUser, CurrentState.DatabasePassword),
                executeAsAdminUser: true
            );

            for (int messageIndex = 0; messageIndex < commandResult.Messages.Count; messageIndex++)
            {
                Log.Info("T-SQL message [{MessageIndex}] from server {ServerId}: {TSqlMessage}",
                    messageIndex,
                    CurrentState.ServerId,
                    commandResult.Messages[messageIndex]
                );
            }

            if (!commandResult.Success)
            {
                foreach (SqlError error in commandResult.Errors)
                {
                    Log.Warning("Error encountered while creating database {DatabaseId} ({DatabaseName}) on server {ServerId} ({ErrorKind}: {ErrorMessage})",
                        CurrentState.Id,
                        CurrentState.Name,
                        CurrentState.ServerId,
                        error.Kind,
                        error.Message
                    );
                }

                throw new SqlExecutionException($"Failed to create database '{CurrentState.Name}' (Id:{CurrentState.Id}) on server {CurrentState.ServerId}.",
                    serverId: CurrentState.ServerId,
                    databaseId: CurrentState.Id,
                    sqlMessages: commandResult.Messages,
                    sqlErrors: commandResult.Errors
                );
            }

            Log.Info("Created database {DatabaseName} (Id:{DatabaseId}) on server {ServerId}.",
                CurrentState.Name,
                CurrentState.Id,
                CurrentState.ServerId
            );
        }

        /// <summary>
        ///     Drop the database.
        /// </summary>
        async Task DropDatabase()
        {
            Log.Info("Dropping database {DatabaseName} (Id:{DatabaseId}) on server {ServerId}...",
                CurrentState.Name,
                CurrentState.Id,
                CurrentState.ServerId
            );

            CommandResult commandResult = await SqlClient.ExecuteCommand(
                serverId: CurrentState.ServerId,
                databaseId: SqlApiClient.MasterDatabaseId,
                sql: ManagementSql.DropDatabase(CurrentState.Name),
                executeAsAdminUser: true
            );

            for (int messageIndex = 0; messageIndex < commandResult.Messages.Count; messageIndex++)
            {
                Log.Info("T-SQL message [{MessageIndex}] from server {ServerId}: {TSqlMessage}",
                    messageIndex,
                    CurrentState.ServerId,
                    commandResult.Messages[messageIndex]
                );
            }

            if (!commandResult.Success)
            {
                foreach (SqlError error in commandResult.Errors)
                {
                    Log.Warning("Error encountered while dropping database {DatabaseId} ({DatabaseName}) on server {ServerId} ({ErrorKind}: {ErrorMessage})",
                        CurrentState.Id,
                        CurrentState.Name,
                        CurrentState.ServerId,
                        error.Kind,
                        error.Message
                    );
                }

                throw new SqlExecutionException($"Failed to drop database '{CurrentState.Name}' (Id:{CurrentState.Id}) on server {CurrentState.ServerId}.",
                    serverId: CurrentState.ServerId,
                    databaseId: CurrentState.Id,
                    sqlMessages: commandResult.Messages,
                    sqlErrors: commandResult.Errors
                );
            }

            Log.Info("Dropped database {DatabaseName} (Id:{DatabaseId}) on server {ServerId}.",
                CurrentState.Name,
                CurrentState.Id,
                CurrentState.ServerId
            );
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
            /// <param name="initialState">
            ///     A <see cref="DatabaseInstance"/> representing the actor's initial state.
            /// </param>
            public Initialize(IActorRef serverManager, IActorRef dataAccess, DatabaseInstance initialState)
            {
                if (serverManager == null)
                    throw new ArgumentNullException(nameof(serverManager));
                
                if (dataAccess == null)
                    throw new ArgumentNullException(nameof(dataAccess));
                
                if (initialState == null)
                    throw new ArgumentNullException(nameof(initialState));

                InitialState = initialState;
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
        ///     
        /// The actor name.
        /// </returns>
        public static string ActorName(string databaseId) => $"database-manager.{databaseId}";
    }
}
