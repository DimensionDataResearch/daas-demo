using Microsoft.Extensions.Logging;
using Raven.Client.Documents;
using Raven.Client.ServerWide;
using Raven.Client.ServerWide.Operations;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DaaSDemo.Provisioning.Provisioners
{
    using Data;
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
        /// <param name="documentStore">
        ///     The RavenDB document store.
        /// </param>
        public RavenDatabaseProvisioner(ILogger<DatabaseProvisioner> logger, IDocumentStore documentStore)
            : base(logger)
        {
            if (documentStore == null)
                throw new ArgumentNullException(nameof(documentStore));
            
            DocumentStore = documentStore;
        }

        /// <summary>
        ///     The RavenDB document store.
        /// </summary>
        IDocumentStore DocumentStore { get; }

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

            const int pageSize = 50;
            
            int start = 1;
            string[] databaseNames;
            do
            {
                databaseNames = await DocumentStore.Admin.Server.GetDatabaseNames(start, pageSize);
                if (databaseNames.Contains(State.Name))
                    return true;

                start += pageSize;
            } while (databaseNames.Length > 0);

            return false;
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

            await DocumentStore.Admin.Server.CreateDatabase(State.Name);

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

            await DocumentStore.Admin.Server.DeleteDatabase(State.Name, hardDelete: true);

            Log.LogInformation("Dropped database {DatabaseName} (Id:{DatabaseId}) on server {ServerId}.",
                State.Name,
                State.Id,
                State.ServerId
            );
        }
    }
}
