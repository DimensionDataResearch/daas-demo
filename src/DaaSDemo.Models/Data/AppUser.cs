using Microsoft.AspNetCore.Identity;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;

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

    /// <summary>
    ///     Represents a claim assigned to a DaaS application user.
    /// </summary>
    public class AppUserClaim
        : IdentityUserClaim<string>
    {
        /// <summary>
        ///     Do not persist Id - unused in RavenDB.
        /// </summary>
        [JsonIgnore]
        public override int Id { get; set; }
    }

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

    /// <summary>
    ///     Represents a login assigned to a DaaS application user.
    /// </summary>
    public class AppUserLogin
        : IdentityUserLogin<string>
    {
    }

    /// <summary>
    ///     Represents an authentication token assigned to a DaaS application user.
    /// </summary>
    public class AppUserToken
        : IdentityUserToken<string>
    {
    }
}
