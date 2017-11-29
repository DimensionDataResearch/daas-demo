using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace DaaSDemo.Provisioning.Provisioners
{
    using DatabaseProxy.Client;
    using Exceptions;
    using Models.Data;
    using Models.DatabaseProxy;

    /// <summary>
    ///     Provisioner for <see cref="DatabaseInstance"/>s hosted in SQL Server.
    /// </summary>
    public sealed class SqlServerDatabaseProvisioner
        : DatabaseProvisioner
    {
        /// <summary>
        ///     Create a new <see cref="SqlServerDatabaseProvisioner"/>.
        /// </summary>
        /// <param name="logger">
        ///     The provisioner's logger.
        /// </param>
        /// <param name="databaseProxyClient">
        ///     The <see cref="DatabaseProxyApiClient"/> used to communicate with the Database Proxy API.
        /// </param>
        public SqlServerDatabaseProvisioner(ILogger<DatabaseProvisioner> logger, DatabaseProxyApiClient databaseProxyClient)
            : base(logger, databaseProxyClient)
        {
        }

        /// <summary>
        ///     Determine whether the provisioner supports the specified server type.
        /// </summary>
        /// <param name="serverKind">
        ///     A <see cref="DatabaseServerKind"/> value representing the server type.
        /// </param>
        /// <returns>
        ///     <c>true</c>, if the provisioner supports databases hosted in the specified server type; otherwise, <c>false</c>.
        /// </returns>
        public override bool SupportsServerKind(DatabaseServerKind serverKind) => serverKind == DatabaseServerKind.SqlServer;

        /// <summary>
        ///     Check if the target database exists.
        /// </summary>
        /// <returns>
        ///     <c>true</c>, if the database exists; otherwise, <c>false</c>.
        /// </returns>
        public override async Task<bool> DoesDatabaseExist()
        {
            RequireState();

            QueryResult result = await DatabaseProxyClient.ExecuteQuery(
                serverId: State.ServerId,
                databaseId: DatabaseProxyApiClient.MasterDatabaseId,
                sql: ManagementSql.CheckDatabaseExists(),
                parameters: ManagementSql.Parameters.CheckDatabaseExists(
                    databaseName: State.Name
                ),
                executeAsAdminUser: true,
                stopOnError: true
            );

            return result.ResultSets[0].Rows.Count == 1;
        }

        /// <summary>
        ///     Create the database.
        /// </summary>
        /// <returns>
        ///     A <see cref="Task"/> representing the operation.
        /// </returns>
        public override async Task CreateDatabase()
        {
            RequireState();

            Log.LogInformation("Creating database {DatabaseName} (Id:{DatabaseId}) on server {ServerId}...",
                State.Name,
                State.Id,
                State.ServerId
            );

            CommandResult commandResult = await DatabaseProxyClient.ExecuteCommand(
                serverId: State.ServerId,
                databaseId: DatabaseProxyApiClient.MasterDatabaseId,
                sql: ManagementSql.CreateDatabase(
                    State.Name,
                    State.DatabaseUser,
                    State.DatabasePassword,
                    maxPrimaryFileSizeMB: State.Storage.SizeMB,
                    maxLogFileSizeMB: (int)(0.2 * State.Storage.SizeMB) // Reserve an additional 20% of storage for transaction logs.
                ),
                executeAsAdminUser: true,
                stopOnError: true
            );

            for (int messageIndex = 0; messageIndex < commandResult.Messages.Count; messageIndex++)
            {
                Log.LogInformation("T-SQL message [{MessageIndex}] from server {ServerId}: {TSqlMessage}",
                    messageIndex,
                    State.ServerId,
                    commandResult.Messages[messageIndex]
                );
            }

            if (!commandResult.Success)
            {
                foreach (SqlError error in commandResult.Errors)
                {
                    Log.LogWarning("Error encountered while creating database {DatabaseId} ({DatabaseName}) on server {ServerId} ({ErrorKind}: {ErrorMessage})",
                        State.Id,
                        State.Name,
                        State.ServerId,
                        error.Kind,
                        error.Message
                    );
                }

                throw new SqlExecutionException($"Failed to create database '{State.Name}' (Id:{State.Id}) on server {State.ServerId}.",
                    serverId: State.ServerId,
                    databaseId: State.Id,
                    sqlMessages: commandResult.Messages,
                    sqlErrors: commandResult.Errors
                );
            }

            Log.LogInformation("Created database {DatabaseName} (Id:{DatabaseId}) on server {ServerId}.",
                State.Name,
                State.Id,
                State.ServerId
            );
        }

        /// <summary>
        ///     Drop the database.
        /// </summary>
        public override async Task DropDatabase()
        {
            RequireState();

            Log.LogInformation("Dropping database {DatabaseName} (Id:{DatabaseId}) on server {ServerId}...",
                State.Name,
                State.Id,
                State.ServerId
            );

            CommandResult commandResult = await DatabaseProxyClient.ExecuteCommand(
                serverId: State.ServerId,
                databaseId: DatabaseProxyApiClient.MasterDatabaseId,
                sql: ManagementSql.DropDatabase(State.Name),
                executeAsAdminUser: true,
                stopOnError: true
            );

            for (int messageIndex = 0; messageIndex < commandResult.Messages.Count; messageIndex++)
            {
                Log.LogInformation("T-SQL message [{MessageIndex}] from server {ServerId}: {TSqlMessage}",
                    messageIndex,
                    State.ServerId,
                    commandResult.Messages[messageIndex]
                );
            }

            if (!commandResult.Success)
            {
                foreach (SqlError error in commandResult.Errors)
                {
                    Log.LogWarning("Error encountered while dropping database {DatabaseId} ({DatabaseName}) on server {ServerId} ({ErrorKind}: {ErrorMessage})",
                        State.Id,
                        State.Name,
                        State.ServerId,
                        error.Kind,
                        error.Message
                    );
                }

                throw new SqlExecutionException($"Failed to drop database '{State.Name}' (Id:{State.Id}) on server {State.ServerId}.",
                    serverId: State.ServerId,
                    databaseId: State.Id,
                    sqlMessages: commandResult.Messages,
                    sqlErrors: commandResult.Errors
                );
            }

            Log.LogInformation("Dropped database {DatabaseName} (Id:{DatabaseId}) on server {ServerId}.",
                State.Name,
                State.Id,
                State.ServerId
            );
        }
    }
}
