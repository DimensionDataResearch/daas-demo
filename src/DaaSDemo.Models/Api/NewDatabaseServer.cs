using System.ComponentModel.DataAnnotations;

namespace DaaSDemo.Models.Api
{
    using Models.Data;

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
        [RegularExpression("[a-zA-Z][a-zA-Z0-9._]{2,}", ErrorMessage = "Server names can only contain letters, numbers, '.', and '_'.")]
        public string Name { get; set; }

        /// <summary>
        ///     The server's administrative ("sa" user) password.
        /// </summary>
        [MinLength(5)]
        [Required(AllowEmptyStrings = false)]
        public string AdminPassword { get; set; }

        /// <summary>
        ///     The kind of database server to create.
        /// </summary>
        [Required]
        public DatabaseServerKind Kind { get; set; }
    }
}
