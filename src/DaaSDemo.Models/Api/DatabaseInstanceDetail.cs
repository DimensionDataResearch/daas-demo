using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DaaSDemo.Models.Api
{
    using Models.Data;

    /// <summary>
    ///     Detailed information about a database instance.
    /// </summary>
    public class DatabaseInstanceDetail
    {
        /// <summary>
        ///     Create new <see cref="DatabaseInstanceDetail"/>.
        /// </summary>
        public DatabaseInstanceDetail()
        {
        }

        /// <summary>
        ///     Create new <see cref="DatabaseInstanceDetail"/>.
        /// </summary>
        /// <param name="databaseId"></param>
        /// <param name="databaseName"></param>
        /// <param name="databaseUser"></param>
        /// <param name="databaseAction"></param>
        /// <param name="databaseStatus"></param>
        /// <param name="serverId"></param>
        /// <param name="serverName"></param>
        /// <param name="serverFQDN"></param>
        /// <param name="serverPort"></param>
        /// <param name="tenantId"></param>
        /// <param name="tenantName"></param>
        public DatabaseInstanceDetail(int databaseId, string databaseName, string databaseUser, ProvisioningAction databaseAction, ProvisioningStatus databaseStatus, int serverId, string serverName, string serverFQDN, int? serverPort, int tenantId, string tenantName)
        {
            Id = databaseId;
            Name = databaseName;
            ServerId = serverId;
            ServerName = serverName;
            Action = databaseAction;
            Status = databaseStatus;
            TenantId = tenantId;
            TenantName = tenantName;

            if (!String.IsNullOrWhiteSpace(serverFQDN) && serverPort != null)
                ConnectionString = $"Data Source=tcp:{serverFQDN}:{serverPort};Initial Catalog={Name};User={databaseUser};Password=<password>";
        }

        /// <summary>
        ///     The tenant Id.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        ///     The database name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///     The Id of the server that hosts the database.
        /// </summary>
        public int ServerId { get; set; }

        /// <summary>
        ///     The name of the server that hosts the database.
        /// </summary>
        public string ServerName { get; set; }

        /// <summary>
        ///     The Id of the tenant that owns the database.
        /// </summary>
        public int TenantId { get; set; }

        /// <summary>
        ///     The name of the tenant that owns the database.
        /// </summary>
        public string TenantName { get; set; }

        /// <summary>
        ///     The database's currently-requested provisioning action (if any).
        /// </summary>
        public ProvisioningAction Action { get; set; }

        /// <summary>
        ///     The database's provisioning status.
        /// </summary>
        public ProvisioningStatus Status { get; set; }

        /// <summary>
        ///     The database connection string.
        /// </summary>
        public string ConnectionString { get; }
    }
}
