using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DaaSDemo.Models.Data
{
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
        ///     Create new <see cref="DatabaseInstanceDetail"/> from the specified <see cref="DatabaseInstance"/>.
        /// </summary>
        /// <param name="databaseInstance">
        ///     The <see cref="DatabaseInstance"/> to copy from.
        /// </param>
        public DatabaseInstanceDetail(DatabaseInstance databaseInstance)
        {
            if (databaseInstance == null)
                throw new ArgumentNullException(nameof(databaseInstance));
            
            Id = databaseInstance.Id;
            Name = databaseInstance.Name;
            Action = databaseInstance.Action;
            Status = databaseInstance.Status;
            ConnectionString = $"Data Source=tcp:{databaseInstance.DatabaseServer.PublicFQDN}:{databaseInstance.DatabaseServer.PublicPort};Initial Catalog={databaseInstance.Name};User={databaseInstance.DatabaseUser};Password=<password>";
        }

        /// <summary>
        ///     The tenant Id.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        ///     The database name.
        /// </summary>
        [MaxLength(200)]
        [Required(AllowEmptyStrings = false)]
        public string Name { get; set; }

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
