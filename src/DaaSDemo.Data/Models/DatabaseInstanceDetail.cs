using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DaaSDemo.Data.Models
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
            ConnectionString = $"Data Source={databaseInstance.DatabaseServer.IngressIP}:{databaseInstance.DatabaseServer.IngressPort};Initial Catalog={databaseInstance.Name};User={databaseInstance.DatabaseUser};Password={databaseInstance.DatabasePassword}";
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
        ///     The database connection string.
        /// </summary>
        public string ConnectionString { get; }
    }
}
