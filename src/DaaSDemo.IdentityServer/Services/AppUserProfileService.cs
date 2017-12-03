using IdentityModel;
using IdentityServer4.Models;
using IdentityServer4.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace DaaSDemo.IdentityServer.Services
{
    using System.Linq;
    using Models.Data;

    public class AppUserProfileService
        : IProfileService
    {
        public AppUserProfileService(ILogger<AppUserProfileService> logger, UserManager<AppUser> userManager)
        {
            if (logger == null)
                throw new ArgumentNullException(nameof(logger));

            if (userManager == null)
                throw new ArgumentNullException(nameof(userManager));
            
            Log = logger;
            UserManager = userManager;
        }

        ILogger Log { get; }

        UserManager<AppUser> UserManager { get; }

        public async Task GetProfileDataAsync(ProfileDataRequestContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            AppUser user = await UserManager.GetUserAsync(context.Subject);
            if (user == null)
            {
                Log.LogWarning("GetProfileDataAsync: Failed to find user.");

                return;
            }

            IList<string> roles = await UserManager.GetRolesAsync(user);
            Log.LogInformation("User {Name} has roles: {@Roles}",
                user.UserName,
                roles
            );
            
            context.AddRequestedClaims(roles.Select(
                role => new Claim("role", role)
            ));

            context.AddRequestedClaims(user.Claims.Select(
                claim => new Claim(claim.ClaimType, claim.ClaimValue)
            ));
        }

        public Task IsActiveAsync(IsActiveContext context)
        {
            Log.LogInformation("IsActiveAsync");

            context.IsActive = true;

            return Task.CompletedTask;
        }
    }
}
