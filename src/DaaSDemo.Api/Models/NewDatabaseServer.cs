using System.ComponentModel.DataAnnotations;

namespace DaaSDemo.Api.Models
{
    /// <summary>
    ///     Model for creation of a new <see cref="Data.Models.DatabaseServer"/>.
    /// </summary>
    public class NewDatabaseServer
    {
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
    }
}
