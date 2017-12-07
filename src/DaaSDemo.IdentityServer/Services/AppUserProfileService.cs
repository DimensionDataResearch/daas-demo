using IdentityModel;
using IdentityServer4.Models;
using IdentityServer4.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Raven.Client.Documents;
using Raven.Client.Documents.Session;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace DaaSDemo.IdentityServer.Services
{
    using Models.Data;

    using IdentityConstants = Common.IdentityConstants;

    /// <summary>
    ///     IdentityServer profile service that sources claims for the user profile from <see cref="AppUser"/> details (including role membership).
    /// </summary>
    public class AppUserProfileService
        : IProfileService
    {
        /// <summary>
        ///     Create a new <see cref="AppUserProfileService"/>.
        /// </summary>
        /// <param name="userManager">
        ///     The ASP.NET Core Identity user-management service.
        /// </param>
        /// <param name="logger">
        ///     The service's logger.
        /// </param>
        public AppUserProfileService(UserManager<AppUser> userManager, IDocumentStore documentStore, ILogger<AppUserProfileService> logger)
        {
            if (logger == null)
                throw new ArgumentNullException(nameof(logger));

            if (userManager == null)
                throw new ArgumentNullException(nameof(userManager));

            if (documentStore == null)
                throw new ArgumentNullException(nameof(documentStore));
            
            Log = logger;
            UserManager = userManager;
            DocumentStore = documentStore;
        }

        /// <summary>
        ///     The service's logger.
        /// </summary>
        ILogger Log { get; }

        /// <summary>
        ///     The ASP.NET Core Identity user-management service.
        /// </summary>
        UserManager<AppUser> UserManager { get; }

        /// <summary>
        ///     The RavenDB document store.
        /// </summary>
        IDocumentStore DocumentStore { get; }

        /// <summary>
        ///     Get profile data for the specified subject.
        /// </summary>
        /// <param name="context">
        ///     A <see cref="ProfileDataRequestContext"/> containing contextual information about the subject and the claims being issued.
        /// </param>
        /// <returns>
        ///     A <see cref="Task"/> representing the operation.
        /// </returns>
        public async Task GetProfileDataAsync(ProfileDataRequestContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            // TODO: Refactor this, breaking the adding of various sets of claims up into separate methods.

            AppUser user = await UserManager.GetUserAsync(context.Subject);
            if (user == null)
            {
                Log.LogWarning("GetProfileDataAsync: Failed to find user.");

                return;
            }

            if (!context.IssuedClaims.Any(claim => claim.Type == JwtClaimTypes.Name))
            {
                context.IssuedClaims.Add(
                    new Claim(JwtClaimTypes.Name, user.UserName)
                );
            }

            // Always surface super-user status (allow relying parties to decide what it means).
            if (user.IsSuperUser)
            {
                context.AddRequestedClaims(new Claim[]
                {
                    new Claim(IdentityConstants.JwtClaimTypes.SuperUser, "true", ClaimValueTypes.Boolean)
                });
            }

            IList<string> roleIds = await UserManager.GetRolesAsync(user);
            
            using (IAsyncDocumentSession session = DocumentStore.OpenAsyncSession())
            {
                IDictionary<string, AppRole> roles = await session.LoadAsync<AppRole>(roleIds);
                
                string[] roleNames = roles.Values
                    .Where(role => role != null)
                    .Select(role => role.Name)
                    .ToArray();

                Log.LogInformation("User {Name} has roles: {@RoleNames}",
                    user.UserName,
                    roleNames
                );

                // Expose their role memberships as claims.
                context.AddRequestedClaims(roleNames.Select(
                    roleName => new Claim(JwtClaimTypes.Role, roleName)
                ));

                // Also add any claims provided by roles they are a member of.
                context.AddRequestedClaims(
                    roles.Values.Where(
                        role => role != null
                    )
                    .SelectMany(
                        role => role.Claims.Select(
                            claim => new Claim(claim.ClaimType, claim.ClaimValue)
                        )
                    )
                );
            }

            // Add access claims (if requested).
            context.AddRequestedClaims(user.TenantAccess.Select(
                access => new Claim(
                    type: $"tenant.{access.Value.ToString().ToLower()}",
                    value: access.Key
                )
            ));

            // Finally, add any claims specific to this user.
            context.AddRequestedClaims(user.Claims.Select(
                claim => new Claim(claim.ClaimType, claim.ClaimValue)
            ));
        }

        /// <summary>
        ///     Determine whether the user is valid or active.
        /// </summary>
        /// <param name="context">
        ///     An <see cref="IsActiveContext"/> containing contextual information about the subject whether it should be treated as active.
        /// </param>
        /// <returns>
        ///     A <see cref="Task"/> representing the operation.
        /// </returns>
        public async Task IsActiveAsync(IsActiveContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            AppUser user = await UserManager.GetUserAsync(context.Subject);
            if (user == null)
            {
                Log.LogWarning("IsActiveAsync: Failed to find user.");
                context.IsActive = false;

                return;
            }

            context.IsActive = !user.LockoutEnabled;
        }
    }
}