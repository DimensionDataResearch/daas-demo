using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace DaaSDemo.Provisioning.Provisioners
{
    using DatabaseProxy.Client;
    using Models.Data;

    /// <summary>
    ///     Provisioner for <see cref="DatabaseInstance"/>s hosted in SQL Server.
    /// </summary>
    public abstract class DatabaseProvisioner
        : Provisioner
    {
        /// <summary>
        ///     Create a new <see cref="DatabaseProvisioner"/>.
        /// </summary>
        /// <param name="logger">
        ///     The provisioner's logger.
        /// </param>
        /// <param name="databaseProxyClient">
        ///     The <see cref="DatabaseProxyApiClient"/> used to communicate with the Database Proxy API.
        /// </param>
        protected DatabaseProvisioner(ILogger<DatabaseProvisioner> logger)
            : base(logger)
        {
        }

        /// <summary>
        ///     A <see cref="DatabaseInstance"/> representing the database's target state.
        /// </summary>
        public DatabaseInstance State { get; set; }

        /// <summary>
        ///     Determine whether the provisioner supports the specified server type.
        /// </summary>
        /// <param name="serverKind">
        ///     A <see cref="DatabaseServerKind"/> value representing the server type.
        /// </param>
        /// <returns>
        ///     <c>true</c>, if the provisioner supports databases hosted in the specified server type; otherwise, <c>false</c>.
        /// </returns>
        public abstract bool SupportsServerKind(DatabaseServerKind serverKind);

        /// <summary>
        ///     Check if the target database exists.
        /// </summary>
        /// <returns>
        ///     <c>true</c>, if the database exists; otherwise, <c>false</c>.
        /// </returns>
        public abstract Task<bool> DoesDatabaseExist();

        /// <summary>
        ///     Create the database.
        /// </summary>
        /// <returns>
        ///     A <see cref="Task"/> representing the operation.
        /// </returns>
        public abstract Task CreateDatabase();

        /// <summary>
        ///     Drop the database.
        /// </summary>
        public abstract Task DropDatabase();

        /// <summary>
        ///     Ensure that <see cref="State"/> is populated and valid.
        /// </summary>
        protected void RequireState()
        {
            if (State == null)
                throw new InvalidOperationException($"Cannot use {GetType().Name} without current state.");
        }
    }
}
