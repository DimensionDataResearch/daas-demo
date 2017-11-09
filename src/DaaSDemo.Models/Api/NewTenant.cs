using System.ComponentModel.DataAnnotations;

namespace DaaSDemo.Models.Api
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
