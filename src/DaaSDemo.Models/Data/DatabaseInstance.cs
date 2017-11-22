using Newtonsoft.Json;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DaaSDemo.Models.Data
{
    /// <summary>
    ///     A database instance owned by a tenant.
    /// </summary>
    [Table("DatabaseInstance")]
    public class DatabaseInstance
        : IDeepCloneable<DatabaseInstance>
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
        ///     The database's storage configuration.
        /// </summary>
        [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Reuse)]
        public DatabaseInstanceStorage Storage { get; } = new DatabaseInstanceStorage();

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

        /// <summary>
        ///     Create a deep clone of the <see cref="DatabaseInstance"/>.
        /// </summary>
        /// <returns>
        ///     The cloned <see cref="DatabaseInstance"/>.
        /// </returns>
        public DatabaseInstance Clone()
        {
            return new DatabaseInstance
            {
                Id = Id,
                ServerId = ServerId,
                TenantId = TenantId,

                Name = Name,
                
                DatabaseUser = DatabaseUser,
                DatabasePassword = DatabasePassword,

                Storage =
                {
                    SizeMB = Storage.SizeMB
                },
                
                Action = Action,
                Status = Status,
            };
        }
    }
}
