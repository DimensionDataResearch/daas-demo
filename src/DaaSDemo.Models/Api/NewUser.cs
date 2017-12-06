using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace DaaSDemo.Models.Api
{
    /// <summary>
    ///     Model for creation of a new <see cref="Models.Data.AppUser"/>.
    /// </summary>
    public class NewUser
    {
        /// <summary>
        ///     The user's name (for display purposes).
        /// </summary>
        [DisplayName("Name")]
        [Required(AllowEmptyStrings = false)]
        public string Name { get; set; }

        /// <summary>
        ///     The user's email address.
        /// </summary>
        [DisplayName("Email address")]
        [Required(AllowEmptyStrings = false)]
        public string Email { get; set; }

        /// <summary>
        ///     The user's password.
        /// </summary>
        [DisplayName("Password")]
        [Required(AllowEmptyStrings = false)]
        public string Password { get; set; }

        /// <summary>
        ///     Confirm the user's password.
        /// </summary>
        [DisplayName("Confirm password")]
        [Required(AllowEmptyStrings = false)]
        public string PasswordConfirmation { get; set; }

        /// <summary>
        ///     Grant the user administrative rights?
        /// </summary>
        [DisplayName("Administrator")]
        public bool IsAdmin { get; set; }
    }
}
