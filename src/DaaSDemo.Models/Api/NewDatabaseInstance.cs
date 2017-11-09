using System.ComponentModel.DataAnnotations;

namespace DaaSDemo.Models.Api
{
    /// <summary>
    ///     Model for creation of a new <see cref="Data.Models.DatabaseInstance"/>.
    /// </summary>
    public class NewDatabaseInstance
    {
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
        [MaxLength(50)]
        [Required(AllowEmptyStrings = false)]
        public string DatabasePassword { get; set; }
    }
}
