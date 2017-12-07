using Raven.Client.Documents.Indexes;
using System.Linq;

namespace DaaSDemo.Data.Indexes
{
    using Models.Api;
    using Models.Data;

    /// <summary>
    ///     Index used to aggregate user details for use in the DaaS API.s
    /// </summary>
    public class AppUserDetails
        : AbstractIndexCreationTask<AppUser, AppUserDetail>
    {
        /// <summary>
        ///     Create a new <see cref="AppUserDetails"/> index definition.
        /// </summary>
        public AppUserDetails()
        {
            Map = users =>
                from user in users
                select new AppUserDetail
                {
                    Id = user.Id,
                    Name = user.DisplayName,
                    EmailAddress = user.Email,

                    IsAdmin = user.IsAdmin,
                    IsSuperUser = user.IsSuperUser,
                    IsLockedOut = user.LockoutEnabled
                };

            StoreAllFields(FieldStorage.Yes);
        }
    }
}
