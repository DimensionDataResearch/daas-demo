using System.ComponentModel.DataAnnotations;

namespace DaaSDemo.Models.Api
{
    using Models.Data;

    /// <summary>
    ///     Base model for creation of a new <see cref="Data.Models.DatabaseServer"/>.
    /// </summary>
    public abstract class NewDatabaseServer
    {
        /// <summary>
        ///     The Id of the tenant that will own the server.
        /// </summary>
        [Required]
        public string TenantId { get; set; }

        /// <summary>
        ///     The server name.
        /// </summary>
        [MaxLength(200)]
        [Required(AllowEmptyStrings = false)]
        [RegularExpression("[a-zA-Z][a-zA-Z0-9._]{2,}", ErrorMessage = "Server names can only contain letters, numbers, '.', and '_'.")]
        public string Name { get; set; }

        /// <summary>
        ///     The kind of database server to create.
        /// </summary>
        public abstract DatabaseServerKind Kind { get; }

        /// <summary>
        ///     The total amount of storage (in MB) to allocate to the server.
        /// </summary>
        [Required]
        [Range(minimum: 10, maximum: 4000)]
        public int SizeMB { get; set; }
    }

    /// <summary>
    ///     Request to create a new SQL Server instance
    /// </summary>
    public class NewSqlServer
        : NewDatabaseServer
    {
        /// <summary>
        ///     The server's administrative ("sa" user) password.
        /// </summary>
        [MinLength(5)]
        [Required(AllowEmptyStrings = false)]
        public string AdminPassword { get; set; }

        /// <summary>
        ///     The kind of database server to create.
        /// </summary>
        public override DatabaseServerKind Kind => DatabaseServerKind.SqlServer;
    }

    /// <summary>
    ///     Request to create a new SQL Server instance
    /// </summary>
    public class NewRavenServer
        : NewDatabaseServer
    {
        /// <summary>
        ///     The kind of database server to create.
        /// </summary>
        public override DatabaseServerKind Kind => DatabaseServerKind.RavenDB;
    }
}
