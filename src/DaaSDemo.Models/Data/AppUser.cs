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
        : IDeepCloneable<AppUser>
    {
        /// <summary>
        ///     The user Id.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        ///     The user's (account) name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///     The user's display name.
        /// </summary>
        public string DisplayName { get; set; }
        
        /// <summary>
        ///     The user's e-mail address.
        /// </summary>
        public string EmailAddress { get; set; }

        /// <summary>
        ///     Has user's e-mail address been confirmed?
        /// </summary>
        public bool IsEmailAddressConfirmed { get; set; }

        /// <summary>
        ///     The user's password hash.
        /// </summary>
        public string PasswordHash { get; set; }

        /// <summary>
        ///     Opaque data used by ASP.NET Core Identity to determine whether the user is up-to-date.
        /// </summary>
        public string SecurityStamp { get; set; }

        /// <summary>
        ///     The user's lockout information
        /// </summary>
        [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Reuse)]
        public AppUserLockout Lockout { get; private set; } = new AppUserLockout();

        /// <summary>
        ///     The Ids of roles that the user is a member of.
        /// </summary>
        [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Reuse)]
        public HashSet<string> RoleIds { get; private set; } = new HashSet<string>();

        /// <summary>
        ///     Create a deep clone of the <see cref="AppUser"/>.
        /// </summary>
        /// <returns>
        ///     The cloned <see cref="AppUser"/>.
        /// </returns>
        public AppUser Clone()
        {
            return new AppUser
            {
                Id = Id,
                Name = Name,
                DisplayName = DisplayName,
                EmailAddress = EmailAddress,
                IsEmailAddressConfirmed = IsEmailAddressConfirmed,
                
                PasswordHash = PasswordHash,
                SecurityStamp = SecurityStamp,
                Lockout = Lockout.Clone(),

                RoleIds = new HashSet<string>(RoleIds)
            };
        }
    }

    /// <summary>
    ///     Lockout information for a DaaS application user.
    /// </summary>
    public class AppUserLockout
        : IDeepCloneable<AppUserLockout>
    {
        public int AccessFailedCount { get; set; }
        public bool IsEnabled { get; set; }
        public DateTimeOffset? EndDate { get; set; }
        
        /// <summary>
        ///     Create a deep clone of the <see cref="AppUserLockout"/>.
        /// </summary>
        /// <returns>
        ///     The cloned <see cref="AppUserLockout"/>.
        /// </returns>
        public AppUserLockout Clone()
        {
            return new AppUserLockout
            {
                AccessFailedCount = AccessFailedCount,
                IsEnabled = IsEnabled,
                EndDate = EndDate
            };
        }
    }
}
