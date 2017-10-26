using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DaaSDemo.Data.Models
{
    /// <summary>
    ///     Represents a mapping between an internal IP address and its corresponding external IP address.
    /// </summary>
    public class IPAddressMapping
    {
        /// <summary>
        ///     The mapping Id.
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>
        ///     The internal IP address.
        /// </summary>
        [Required]
        [MaxLength(16)]
        public string InternalIP { get; set; }

        /// <summary>
        ///     The external IP address.
        /// </summary>
        [Required]
        [MaxLength(16)]
        public string ExternalIP { get; set; }
    }
}
