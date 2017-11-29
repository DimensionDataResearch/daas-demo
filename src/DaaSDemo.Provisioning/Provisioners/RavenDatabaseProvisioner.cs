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
    public sealed class RavenDatabaseProvisioner
        : DatabaseProvisioner
    {
        /// <summary>
        ///     Create a new <see cref="RavenDatabaseProvisioner"/>.
        /// </summary>
        /// <param name="logger">
        ///     The provisioner's logger.
        /// </param>
        /// <param name="databaseProxyClient">
        ///     The <see cref="DatabaseProxyApiClient"/> used to communicate with the Database Proxy API.
        /// </param>
        public RavenDatabaseProvisioner(ILogger<DatabaseProvisioner> logger, DatabaseProxyApiClient databaseProxyClient)
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
        public override bool SupportsServerKind(DatabaseServerKind serverKind) => serverKind == DatabaseServerKind.RavenDB;

        /// <summary>
        ///     Check if the target database exists.
        /// </summary>
        /// <returns>
        ///     <c>true</c>, if the database exists; otherwise, <c>false</c>.
        /// </returns>
        public override async Task<bool> DoesDatabaseExist()
        {
            RequireState();

            // TODO: Implement.

            await Task.Yield();

            return false;
        }

        /// <summary>
        ///     Create the database.
        /// </summary>
        /// <returns>
        ///     A <see cref="Task"/> representing the operation.
        /// </returns>
        public override Task CreateDatabase()
        {
            RequireState();

            Log.LogInformation("Creating database {DatabaseName} (Id:{DatabaseId}) on server {ServerId}...",
                State.Name,
                State.Id,
                State.ServerId
            );

            // TODO: Implement.

            Log.LogInformation("Created database {DatabaseName} (Id:{DatabaseId}) on server {ServerId}.",
                State.Name,
                State.Id,
                State.ServerId
            );

            return Task.CompletedTask;
        }

        /// <summary>
        ///     Drop the database.
        /// </summary>
        public override Task DropDatabase()
        {
            RequireState();

            Log.LogInformation("Dropping database {DatabaseName} (Id:{DatabaseId}) on server {ServerId}...",
                State.Name,
                State.Id,
                State.ServerId
            );

            // TODO: Implement.

            Log.LogInformation("Dropped database {DatabaseName} (Id:{DatabaseId}) on server {ServerId}.",
                State.Name,
                State.Id,
                State.ServerId
            );

            return Task.CompletedTask;
        }
    }
}
