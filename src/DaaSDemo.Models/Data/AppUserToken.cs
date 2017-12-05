using Microsoft.AspNetCore.Identity;

namespace DaaSDemo.Models.Data
{
    /// <summary>
    ///     Represents an authentication token assigned to a DaaS application user.
    /// </summary>
    public class AppUserToken
        : IdentityUserToken<string>
    {
    }
}
