using System;
using System.Collections.Generic;
using System.Linq;

namespace DaaSDemo.Models.Api
{
    using Data;

    /// <summary>
    ///     Detailed information about an application user.
    /// </summary>
    public class AppUserDetail
    {
        /// <summary>
        ///     The user's Id.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        ///     The user's (display) name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///     The user's email address.
        /// </summary>
        /// <summary>
        ///     Users log in with this (not their name).
        /// </summary>
        public string EmailAddress { get; set; }

        /// <summary>
        ///     Does the user have administrative rights?
        /// </summary>
        public bool IsAdmin { get; set; }

        /// <summary>
        ///     Is the user a super-user?
        /// </summary>
        public bool IsSuperUser { get; set; }

        /// <summary>
        ///     Is the user's account locked out?
        /// </summary>
        public bool IsLockedOut { get; set; }

        /// <summary>
        ///     The Ids of tenants to which the user has read-only access.
        /// </summary>
        public List<string> ReadTenantIds { get; set; } = new List<string>();

        /// <summary>
        ///     The Ids of tenants to which the user has read / write access.
        /// </summary>
        public List<string> ReadWriteTenantIds { get; set; } = new List<string>();

        /// <summary>
        ///     The Ids of tenants to which the user has owner access.
        /// </summary>
        public List<string> OwnerTenantIds { get; set; } = new List<string>();

        /// <summary>
        ///     Create an <see cref="AppUserDetail"/> from an <see cref="AppUser"/>.
        /// </summary>
        /// <param name="user">
        ///     The <see cref="AppUser"/> to copy from.
        /// </param>
        /// <returns>
        ///     The new <see cref="AppUserDetail"/>.
        /// </returns>
        public static AppUserDetail From(AppUser user)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));
            
            return new AppUserDetail
            {
                Id = user.Id,
                Name = user.DisplayName,
                EmailAddress = user.Email,

                ReadTenantIds = user.ReadTenantIds.ToList(),
                ReadWriteTenantIds = user.ReadWriteTenantIds.ToList(),
                OwnerTenantIds = user.OwnerTenantIds.ToList(),

                IsAdmin = user.IsAdmin,
                IsSuperUser = user.IsSuperUser,
                IsLockedOut = user.LockoutEnabled
            };
        }
    }
}
