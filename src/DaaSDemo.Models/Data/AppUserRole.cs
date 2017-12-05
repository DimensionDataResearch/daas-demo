using Microsoft.AspNetCore.Identity;
using Newtonsoft.Json;

namespace DaaSDemo.Models.Data
{
    /// <summary>
    ///     Represents a DaaS application user's membership of a role.
    /// </summary>
    public class AppUserRole
        : IdentityUserRole<string>
    {
        /// <summary>
        ///     Do not persist user Id - unused in RavenDB.
        /// </summary>
        [JsonIgnore]
        public override string UserId { get; set; }

        /// <summary>
        ///     The name of the role that the user is a member of.
        /// </summary>
        public string RoleName { get; set; }

        /// <summary>
        ///     The normalised name of the role that the user is a member of.
        /// </summary>
        public string NormalizedRoleName { get; set; }
    }
}
