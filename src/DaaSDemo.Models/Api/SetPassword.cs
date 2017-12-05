using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace DaaSDemo.Models.Api
{
    /// <summary>
    ///     Request body when setting a user's password.
    /// </summary>
    public class SetPassword
    {
        /// <summary>
        ///     The user's new password.
        /// </summary>
        [DisplayName("New password")]
        [Required(AllowEmptyStrings = false)]
        public string NewPassword { get; set; }

        /// <summary>
        ///     The user's new password (for confirmation).
        /// </summary>
        [DisplayName("Confirm new password")]
        [Required(AllowEmptyStrings = false)]
        public string NewPasswordConfirmation { get; set; }
    }
}
