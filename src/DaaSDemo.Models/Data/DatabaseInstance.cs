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
    {
        /// <summary>
        ///     The tenant Id.
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

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
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string DatabasePassword { get; set; }

        /// <summary>
        ///     The desired action for the server.
        /// </summary>
        [Required]
        [DefaultValue(ProvisioningAction.None)]
        public ProvisioningAction Action { get; set; }

        /// <summary>
        ///     The current status of the server.
        /// </summary>
        [Required]
        [DefaultValue(ProvisioningStatus.Pending)]
        public ProvisioningStatus Status { get; set; }

        /// <summary>
        ///     The Id of the tenant's database server (if any).
        /// </summary>
        [Required]
        [ForeignKey("DatabaseServer")]
        public int DatabaseServerId { get; set; }

        /// <summary>
        ///     The tenant's database server (if any).
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DatabaseServer DatabaseServer { get; set; }

        /// <summary>
        ///     Does the database's server have ingress details available?
        /// </summary>
        /// <returns>
        ///     <c>true</c>, if <see cref="DatabaseServer"/> is not <c>null</c>, and has both <see cref="DatabaseServer.PublicFQDN"/> and <see cref="DatabaseServer.PublicPort"/>; otherwise, <c>false</c>.
        /// </returns>
        public bool DoesServerHaveIngress() => DatabaseServer?.PublicFQDN != null && DatabaseServer?.PublicPort != null;

        /// <summary>
        ///     Get the connection string for the database.
        /// </summary>
        /// <returns>
        ///     The connection string.
        /// </returns>
        public string GetConnectionString() => $"Data Source={DatabaseServer?.PublicFQDN},{DatabaseServer?.PublicPort};Initial Catalog={Name};User=sa;Password={DatabaseServer?.AdminPassword}";
    }
}
