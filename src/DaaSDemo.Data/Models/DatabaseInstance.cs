using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DaaSDemo.Data.Models
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
        ///     The name of database-level user.
        /// </summary>
        [MaxLength(50)]
        [Required(AllowEmptyStrings = false)]
        public string DatabaseUser { get; set; }

        /// <summary>
        ///     The password for the database-level user.
        /// </summary>
        [MaxLength(50)]
        [Required(AllowEmptyStrings = false)]
        public string DatabasePassword { get; set; }

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
    }
}