using System.ComponentModel.DataAnnotations;

namespace DaaSDemo.Api.Models
{
    /// <summary>
    ///     Model for creation of a new <see cref="Data.Models.Tenant"/>.
    /// </summary>
    public class NewTenant
    {
        /// <summary>
        ///     The tenant name.
        /// </summary>
        [MaxLength(200)]
        [Required(AllowEmptyStrings = false)]
        public string Name { get; set; }
    }
}
