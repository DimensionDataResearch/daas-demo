using Newtonsoft.Json;
using System.ComponentModel;
using System.Collections.Generic;

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
        ///     The user's password.
        /// </summary>
        /// <remarks>
        ///     TODO: Store this as a hash (hopefully ASP.NET Core Identity can handle that for us).
        /// </remarks>
        public string Password { get; set; }

        /// <summary>
        ///     The user's display name.
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        ///     The Ids of roles that the user is a member of.
        /// </summary>
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

                RoleIds = new HashSet<string>(RoleIds)
            };
        }
    }
}
