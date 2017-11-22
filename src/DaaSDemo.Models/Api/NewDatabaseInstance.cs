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
        [MinLength(3)]
        [MaxLength(200)]
        [Required(AllowEmptyStrings = false)]
        [RegularExpression("[a-zA-Z][a-zA-Z0-9._]{2,}", ErrorMessage = "Database names can only contain letters, numbers, '.', and '_'.")]
        public string Name { get; set; }

        /// <summary>
        ///     The Id of the database server that will host the database.
        /// </summary>
        [Required(AllowEmptyStrings = false)]
        public string ServerId { get; set; }

        /// <summary>
        ///     The name of the database-level user.
        /// </summary>
        [MinLength(3)]
        [MaxLength(50)]
        [Required(AllowEmptyStrings = false)]
        [RegularExpression("[a-zA-Z][a-zA-Z0-9_]{2,}", ErrorMessage = "User names can only contain letters, numbers, and '_'.")]
        public string DatabaseUser { get; set; }

        /// <summary>
        ///     The password for the database-level user.
        /// </summary>
        [MinLength(5)]
        [MaxLength(50)]
        [Required(AllowEmptyStrings = false)]
        public string DatabasePassword { get; set; }

        /// <summary>
        ///     The amount of storage (in MB) to allocate to the database.
        /// </summary>
        [Required]
        [Range(minimum: 10, maximum: 4000)]
        public int StorageMB { get; set; }
    }
}
