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
        ///     The <see cref="DatabaseInstance"/> to copy from.
        /// </param>
        public DatabaseInstanceDetail(DatabaseInstance database)
        {
            if (database == null)
                throw new ArgumentNullException(nameof(database));

            Id = database.Id;
            Name = database.Name;
            Action = database.Action;
            Status = database.Status;
            ConnectionString = $"Data Source=tcp:{database.DatabaseServer.PublicFQDN}:{database.DatabaseServer.PublicPort};Initial Catalog={database.Name};User={database.DatabaseUser};Password=<password>";
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
