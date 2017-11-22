using Newtonsoft.Json;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DaaSDemo.Models.Data
{
    /// <summary>
    ///     A database instance owned by a tenant.
    /// </summary>
    [Table("database-instance")]
    public class DatabaseInstance
    {
        /// <summary>
        ///     The tenant Id.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        ///     The database name.
        /// </summary>
        [MaxLength(200)]
        [Required(AllowEmptyStrings = false)]
        public string Name { get; set; }

        /// <summary>
        ///     The name of the database-level user.
        /// </summary>
        [MaxLength(50)]
        [Required(AllowEmptyStrings = false)]
        public string DatabaseUser { get; set; }

        /// <summary>
        ///     The password for the database-level user.
        /// </summary>
        [JsonIgnore]
        [MaxLength(50)]
        [Required(AllowEmptyStrings = false)]
        public string DatabasePassword { get; set; }

        /// <summary>
        ///     The desired action for the server.
        /// </summary>
        public ProvisioningAction Action { get; set; } = ProvisioningAction.None;

        /// <summary>
        ///     The current status of the server.
        /// </summary>
        public ProvisioningStatus Status { get; set; } = ProvisioningStatus.Pending;

        /// <summary>
        ///     The Id of the tenant that owns the database.
        /// </summary>
        [Required]
        public string TenantId { get; set; }

        /// <summary>
        ///     The Id of the tenant's database server (if any).
        /// </summary>
        [Required]
        public string ServerId { get; set; }
    }
}
