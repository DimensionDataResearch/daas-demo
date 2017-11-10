using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace DaaSDemo.Models.Data
{
    /// <summary>
    ///     Represents a database tenant.
    /// </summary>
    [Table("Tenant")]
    public class Tenant
    {
        /// <summary>
        ///     The tenant Id.
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>
        ///     The tenant name.
        /// </summary>
        [MaxLength(200)]
        [Required(AllowEmptyStrings = false)]
        public string Name { get; set; }

        /// <summary>
        ///     The Id of the tenant's database server (if any).
        /// </summary>
        public int? DatabaseServerId { get; set; }

        /// <summary>
        ///     The tenant's database server (if any).
        /// </summary>
        [ForeignKey("DatabaseServerId")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DatabaseServer DatabaseServer { get; set; }
    }
}
