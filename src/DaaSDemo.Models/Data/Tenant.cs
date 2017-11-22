using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace DaaSDemo.Models.Data
{
    /// <summary>
    ///     Represents a database tenant.
    /// </summary>
    [EntitySet("tenant")]
    public class Tenant
    {
        /// <summary>
        ///     The tenant Id.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        ///     The tenant name.
        /// </summary>
        [MaxLength(200)]
        [Required(AllowEmptyStrings = false)]
        public string Name { get; set; }
    }
}
