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
        ///     Is the user a superuser?
        /// </summary>
        /// <remarks>
        ///     Superusers cannot be deleted, and have all rights (globally).
        /// </remarks>
        public bool IsSuperUser { get; set; }

        /// <summary>
        ///     Is the user an Administrator?
        /// </summary>
        public bool IsAdmin => Roles.Any(userRole => userRole.RoleName == "Administrator");

        /// <summary>
        ///     The user's access levels for their tenants, keyed by tenant Id.
        /// </summary>
        [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Reuse)]
        public Dictionary<string, TenantAccessLevel> TenantAccess { get; } = new Dictionary<string, TenantAccessLevel>();

        /// <summary>
        ///     The Ids of tenants to which the user has read-only access.
        /// </summary>
        public IEnumerable<string> ReadTenantIds => TenantAccess.Where(access => access.Value == TenantAccessLevel.Read).Select(access => access.Key);

        /// <summary>
        ///     The Ids of tenants to which the user has read / write access.
        /// </summary>
        public IEnumerable<string> ReadWriteTenantIds => TenantAccess.Where(access => access.Value == TenantAccessLevel.ReadWrite).Select(access => access.Key);

        /// <summary>
        ///     The Ids of tenants to which the user has owner-level access.
        /// </summary>
        public IEnumerable<string> OwnerTenantIds => TenantAccess.Where(access => access.Value == TenantAccessLevel.Owner).Select(access => access.Key);

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
