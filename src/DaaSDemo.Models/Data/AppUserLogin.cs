using Microsoft.AspNetCore.Identity;

namespace DaaSDemo.Models.Data
{
    /// <summary>
    ///     Represents a login assigned to a DaaS application user.
    /// </summary>
    public class AppUserLogin
        : IdentityUserLogin<string>
    {
    }
}
