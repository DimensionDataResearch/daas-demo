using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DaaSDemo.Models.Data
{
    /// <summary>
    ///     Represents a database server allocated to a tenant.
    /// </summary>
    [Table("DatabaseServer")]
    public class DatabaseServer
    {
        /// <summary>
        ///     The server Id.
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>
        ///     The server (container / deployment) name.
        /// </summary>
        [MaxLength(200)]
        [Required(AllowEmptyStrings = false)]
        public string Name { get; set; }

        /// <summary>
        ///     The server's administrative ("sa" user) password.
        /// </summary>
        [Required(AllowEmptyStrings = false)]
        public string AdminPassword { get; set; }

        /// <summary>
        ///     The external IP address that the server is listening on.
        /// </summary>
        [MaxLength(100)]
        public string PublicFQDN { get ; set; }

        /// <summary>
        ///     The external port that the server is listening on.
        /// </summary>
        public int? PublicPort { get; set; }

        /// <summary>
        ///     The Id of the tenant that owns the database server.
        /// </summary>
        [Required]
        public int TenantId { get; set; }

        /// <summary>
        ///     The tenant that owns the database server.
        /// </summary>
        [ForeignKey("TenantId")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public Tenant Tenant { get; set; }

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
        ///     The server's current provisioning phase.
        /// </summary>
        [Required]
        [JsonIgnore]
        [DefaultValue(ServerProvisioningPhase.None)]
        public ServerProvisioningPhase Phase { get; set; }

        /// <summary>
        ///     Databases present on the server.
        /// </summary>
        [JsonIgnore]
        [InverseProperty("DatabaseServer")]
        public ICollection<DatabaseInstance> Databases { get; set; } = new HashSet<DatabaseInstance>();

        /// <summary>
        ///     Get the connection string for the DaaS master database.
        /// </summary>
        /// <returns>
        ///     The connection string.
        /// </returns>
        public string GetMasterConnectionString() => $"Data Source={PublicFQDN},{PublicPort};Initial Catalog=master;User=sa;Password={AdminPassword}";
    }
}
