using Akka;
using Akka.Actor;
using HTTPlease;
using KubeNET.Swagger.Model;
using Microsoft.EntityFrameworkCore;
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
        ///     A reference to the <see cref="TenantServerManager"/> actor.
        /// </summary>
        readonly IActorRef _serverManager;

        /// <summary>
        ///     A reference to the <see cref="DataAccess"/> actor.
        /// </summary>
        readonly IActorRef _dataAccess;

        /// <summary>
        ///     The <see cref="SqlApiClient"/> used to communicate with the SQL executor API.
        /// </summary>
        SqlApiClient _sqlClient;

        /// <summary>
        ///     Create a new <see cref="TenantDatabaseManager"/>.
        /// </summary>
        /// <param name="serverManager">
        ///     A reference to the <see cref="TenantServerManager"/> actor.
        /// </param>
        /// <param name="dataAccess">
        ///     A reference to the <see cref="DataAccess"/> actor.
        /// </param>
        public TenantDatabaseManager(IActorRef serverManager, IActorRef dataAccess)
        {
            if (serverManager == null)
                throw new ArgumentNullException(nameof(serverManager));

            if (dataAccess == null)
                throw new ArgumentNullException(nameof(dataAccess));

            _serverManager = serverManager;
            _dataAccess = dataAccess;
            _sqlClient = CreateSqlApiClient();

            ReceiveAsync<DatabaseInstance>(async database =>
            {
                CurrentState = database;

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
        ///     A <see cref="DatabaseInstance"/> representing the currently-desired database state.
        /// </summary>
        DatabaseInstance CurrentState { get; set; }

        /// <summary>
        ///     Called when the actor is stopped.
        /// </summary>
        protected override void PostStop()
        {
            if (_sqlClient != null)
            {
                _sqlClient.Dispose();
                _sqlClient = null;
            }
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
                CurrentState.DatabaseServerId
            );

            _dataAccess.Tell(
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

                _dataAccess.Tell(
                    new DatabaseProvisioned(CurrentState.Id)
                );
            }
            catch (ProvisioningException createDatabaseFailed)
            {
                Log.Error(createDatabaseFailed, "Unexpected error creating database {DatabaseName} (Id:{DatabaseId}).",
                    CurrentState.Name,
                    CurrentState.Id
                );

                _dataAccess.Tell(
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
                CurrentState.DatabaseServerId
            );

            _dataAccess.Tell(
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

                _dataAccess.Tell(
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
            QueryResult result = await _sqlClient.ExecuteQuery(
                serverId: CurrentState.DatabaseServerId,
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
            Log.Info("Creating database {DatabaseName} (Id:{DatabaseId}) on server {ServerName} (Id:{ServerId})...",
                CurrentState.Name,
                CurrentState.Id,
                CurrentState.DatabaseServer.Name,
                CurrentState.DatabaseServer.Id
            );

            CommandResult commandResult = await _sqlClient.ExecuteCommand(
                serverId: CurrentState.DatabaseServerId,
                databaseId: SqlApiClient.MasterDatabaseId,
                sql: ManagementSql.CreateDatabase(CurrentState.Name, CurrentState.DatabaseUser, CurrentState.DatabasePassword),
                executeAsAdminUser: true
            );

            for (int messageIndex = 0; messageIndex < commandResult.Messages.Count; messageIndex++)
            {
                Log.Info("T-SQL message [{MessageIndex}] from server {ServerId}: {TSqlMessage}",
                    messageIndex,
                    CurrentState.DatabaseServerId,
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
                        CurrentState.DatabaseServerId,
                        error.Kind,
                        error.Message
                    );
                }

                throw new SqlExecutionException($"Failed to create database '{CurrentState.Name}' (Id:{CurrentState.Id}) on server '{CurrentState.DatabaseServer.Name}' (Id:{CurrentState.DatabaseServer.Id}).",
                    serverId: CurrentState.DatabaseServerId,
                    databaseId: CurrentState.Id,
                    sqlMessages: commandResult.Messages,
                    sqlErrors: commandResult.Errors
                );
            }

            Log.Info("Created database {DatabaseName} (Id:{DatabaseId}) on server {ServerName} (Id:{ServerId}).",
                CurrentState.Name,
                CurrentState.Id,
                CurrentState.DatabaseServer.Name,
                CurrentState.DatabaseServer.Id
            );
        }

        /// <summary>
        ///     Drop the database.
        /// </summary>
        async Task DropDatabase()
        {
            Log.Info("Dropping database {DatabaseName} (Id:{DatabaseId}) on server {ServerName} (Id:{ServerId})...",
                CurrentState.Name,
                CurrentState.Id,
                CurrentState.DatabaseServer.Name,
                CurrentState.DatabaseServer.Id
            );

            CommandResult commandResult = await _sqlClient.ExecuteCommand(
                serverId: CurrentState.DatabaseServerId,
                databaseId: SqlApiClient.MasterDatabaseId,
                sql: ManagementSql.DropDatabase(CurrentState.Name),
                executeAsAdminUser: true
            );

            for (int messageIndex = 0; messageIndex < commandResult.Messages.Count; messageIndex++)
            {
                Log.Info("T-SQL message [{MessageIndex}] from server {ServerId}: {TSqlMessage}",
                    messageIndex,
                    CurrentState.DatabaseServerId,
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
                        CurrentState.DatabaseServerId,
                        error.Kind,
                        error.Message
                    );
                }

                throw new SqlExecutionException($"Failed to drop database '{CurrentState.Name}' (Id:{CurrentState.Id}) on server '{CurrentState.DatabaseServer.Name}' (Id:{CurrentState.DatabaseServer.Id}).",
                    serverId: CurrentState.DatabaseServerId,
                    databaseId: CurrentState.Id,
                    sqlMessages: commandResult.Messages,
                    sqlErrors: commandResult.Errors
                );
            }

            Log.Info("Dropped database {DatabaseName} (Id:{DatabaseId}) on server {ServerName} (Id:{ServerId}).",
                CurrentState.Name,
                CurrentState.Id,
                CurrentState.DatabaseServer.Name,
                CurrentState.DatabaseServer.Id
            );
        }

        /// <summary>
        ///     Create a new <see cref="SqlApiClient"/> for communicating with the SQL Executor API.
        /// </summary>
        /// <returns>
        ///     The configured <see cref="SqlApiClient"/>.
        /// </returns>
        SqlApiClient CreateSqlApiClient()
        {
            return SqlApiClient.Create(
                endPointUri: new Uri(
                    Context.System.Settings.Config.GetString("daas.sql.api-endpoint")
                )
            );
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
        public static string ActorName(int databaseId) => $"database-manager.{databaseId}";
    }
}
