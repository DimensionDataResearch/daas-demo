using System;
using System.Linq;
using System.Security.Claims;

namespace DaaSDemo.Identity
{
    using Common;
    using Models.Data;

    /// <summary>
    ///     Extension methods for <see cref="ClaimsPrincipal"/> and friends.
    /// </summary>
    public static class ClaimExtensions
    {
        /// <summary>
        ///     Determine whether the <see cref="ClaimsPrincipal"/> is an authenticated user.
        /// </summary>
        /// <param name="principal">
        ///     The <see cref="ClaimsPrincipal"/> to examine.
        /// </param>
        /// <returns>
        ///     <c>true</c>, if the <see cref="ClaimsPrincipal"/> is a member of the User role; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsUser(this ClaimsPrincipal principal)
        {
            if (principal == null)
                throw new ArgumentNullException(nameof(principal));
            
            return principal.Identity.IsAuthenticated && principal.IsInRole("User");
        }

        /// <summary>
        ///     Determine whether the <see cref="ClaimsPrincipal"/> has administrative rights.
        /// </summary>
        /// <param name="principal">
        ///     The <see cref="ClaimsPrincipal"/> to examine.
        /// </param>
        /// <returns>
        ///     <c>true</c>, if the <see cref="ClaimsPrincipal"/> is a member of the Administrator role; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsAdministrator(this ClaimsPrincipal principal)
        {
            if (principal == null)
                throw new ArgumentNullException(nameof(principal));
            
            return principal.Identity.IsAuthenticated && principal.IsInRole("Administrator");
        }

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
            
            return principal.Identity.IsAuthenticated && principal.FindFirstValue(IdentityConstants.JwtClaimTypes.SuperUser) == "true";
        }

        /// <summary>
        ///     Determine whether the principal represented by the <see cref="ClaimsPrincipal"/> has the specified level of access to the specified tenant.
        /// </summary>
        /// <param name="principal">
        ///     The <see cref="ClaimsPrincipal"/> to examine.
        /// </param>
        /// <param name="tenantId">
        ///     The Id of the target tenant.
        /// </param>
        /// <param name="accessLevel">
        ///     The desired access level.
        /// </param>
        /// <returns>
        ///     <c>true</c>, if the tenant has access to the tenant at a level greater than or equal to the specified access level; otherwise, <c>false</c>.
        /// </returns>
        public static bool HasAccessToTenant(this ClaimsPrincipal principal, string tenantId, TenantAccessLevel accessLevel)
        {
            if (principal == null)
                throw new ArgumentNullException(nameof(principal));
            
            if (String.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentException("Argument cannot be null, empty, or entirely composed of whitespace: 'tenantId'.", nameof(tenantId));

            if (!principal.Identity.IsAuthenticated)
                return false;
            
            const string tenantAccessClaimTypePrefix = "tenant.";

            TenantAccessLevel[] accessLevels =
                principal.Claims.Where(
                    claim => claim.Type.StartsWith(tenantAccessClaimTypePrefix) && claim.Value == tenantId
                )
                .Select(
                    claim => (TenantAccessLevel)Enum.Parse(typeof(TenantAccessLevel),
                        claim.Type.Substring(tenantAccessClaimTypePrefix.Length + 1),
                        ignoreCase: true
                    )
                )
                .ToArray();

            return accessLevels.Any(level => level >= accessLevel);
        }
    }
}
