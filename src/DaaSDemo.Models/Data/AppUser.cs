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
        ///     Claims assigned to the user.
        /// </summary>
        [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Reuse)]
        public HashSet<AppUserClaim> Claims { get; } = new HashSet<AppUserClaim>();

        /// <summary>
        ///     Logins assigned to the user.
        /// </summary>
        [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Reuse)]
        public HashSet<AppUserLogin> UserLogins { get; } = new HashSet<AppUserLogin>();

        /// <summary>
        ///     AF: I don't see the distinction.
        /// </summary>
        [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Reuse)]
        public HashSet<UserLoginInfo> Logins { get; } = new HashSet<UserLoginInfo>();

        /// <summary>
        ///     Tokens assigned to the user.
        /// </summary>
        [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Reuse)]
        public HashSet<AppUserToken> Tokens { get; } = new HashSet<AppUserToken>();
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
