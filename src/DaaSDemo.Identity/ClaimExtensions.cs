using System;
using System.Security.Claims;

namespace DaaSDemo.Identity
{
    using Common;

    /// <summary>
    ///     Extension methods for <see cref="ClaimsPrincipal"/> and friends.
    /// </summary>
    public static class ClaimExtensions
    {
        /// <summary>
        ///     Determine whether the <see cref="ClaimsPrincipal"/> represents a super-user.
        /// </summary>
        /// <param name="principal">
        ///     The <see cref="ClaimsPrincipal"/> to examine.
        /// </param>
        /// <returns>
        ///     <c>true</c>, if the <see cref="ClaimsPrincipal"/> represents a super-user; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsSuperUser(this ClaimsPrincipal principal)
        {
            if (principal == null)
                throw new ArgumentNullException(nameof(principal));
            
            return principal.FindFirstValue(IdentityConstants.JwtClaimTypes.SuperUser) == "true";
        }
    }
}