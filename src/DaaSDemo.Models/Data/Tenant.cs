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
    }
}
