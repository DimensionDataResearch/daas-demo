using Microsoft.AspNetCore.Identity;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace DaaSDemo.Models.Data
{
    /// <summary>
    ///     A DaaS application user.
    /// </summary>
    [EntitySet("AppUser")]
    public class AppUser
        : IdentityUser<string>
    {
        /// <summary>
        ///     The user's name (for display purposes).
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        ///     Is the user an Administrator?
        /// </summary>
        public bool IsAdmin => Roles.Any(userRole => userRole.RoleName == "Administrator");

        /// <summary>
        ///     Roles assigned to the user.
        /// </summary>
        [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Reuse)]
        public List<AppUserRole> Roles { get; } = new List<AppUserRole>();

        /// <summary>
        ///     Claims assigned to the user.
        /// </summary>
        [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Reuse)]
        public List<AppUserClaim> Claims { get; } = new List<AppUserClaim>();

        /// <summary>
        ///     Logins assigned to the user.
        /// </summary>
        [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Reuse)]
        public List<AppUserLogin> UserLogins { get; } = new List<AppUserLogin>();

        /// <summary>
        ///     AF: I don't see the distinction.
        /// </summary>
        [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Reuse)]
        public List<UserLoginInfo> Logins { get; } = new List<UserLoginInfo>();

        /// <summary>
        ///     Tokens assigned to the user.
        /// </summary>
        [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Reuse)]
        public List<AppUserToken> Tokens { get; } = new List<AppUserToken>();
    }
}
