using Newtonsoft.Json;
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
        /// <param name="database">
        ///     The underlying <see cref="DatabaseInstance"/>.
        /// </param>
        /// <param name="server">
        ///     A <see cref="DatabaseServer"/> representing the server that hosts the database.
        /// </param>
        /// <param name="tenant">
        ///     A <see cref="Tenant"/> representing the database's owning tenant.
        /// </param>
        public DatabaseInstanceDetail(DatabaseInstance database, DatabaseServer server, Tenant tenant)
        {
            if (database == null)
                throw new ArgumentNullException(nameof(database));
            
            if (server == null)
                throw new ArgumentNullException(nameof(server));
            
            if (tenant == null)
                throw new ArgumentNullException(nameof(tenant));
            
            Id = database.Id;
            Name = database.Name;

            StorageMB = database.Storage.SizeMB;

            Action = database.Action;
            Status = database.Status;

            ServerId = server.Id;
            ServerName = server.Name;

            TenantId = tenant.Id;
            TenantName = tenant.Name;

            if (!String.IsNullOrWhiteSpace(server.PublicFQDN) && server.PublicPort != null)
                ConnectionString = $"Data Source=tcp:{server.PublicFQDN}:{server.PublicPort};Initial Catalog={Name};User={database.DatabaseUser};Password=<password>";
        }

        /// <summary>
        ///     The tenant Id.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        ///     The database name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///     The Id of the server that hosts the database.
        /// </summary>
        public string ServerId { get; set; }

        /// <summary>
        ///     The name of the server that hosts the database.
        /// </summary>
        public string ServerName { get; set; }

        /// <summary>
        ///     The Id of the tenant that owns the database.
        /// </summary>
        public string TenantId { get; set; }

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
        ///     The amount of storage (in MB) allocated to the database.
        /// </summary>
        [Required]
        public int StorageMB { get; set; }

        /// <summary>
        ///     The database connection string.
        /// </summary>
        public string ConnectionString { get; set; }
    }
}
