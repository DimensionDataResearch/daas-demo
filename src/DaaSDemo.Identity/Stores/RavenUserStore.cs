using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Raven.Client.Documents;
using Raven.Client.Documents.Session;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace DaaSDemo.Identity.Stores
{
    using Models.Data;

    public class RavenUserStore
        : UserStoreBase<AppUser, string, AppUserClaim, AppUserLogin, AppUserToken>, IUserStore<AppUser>, IUserAuthenticationTokenStore<AppUser>, IUserAuthenticatorKeyStore<AppUser>, IUserClaimStore<AppUser>, IUserEmailStore<AppUser>, IUserLockoutStore<AppUser>, IUserLoginStore<AppUser>, IUserPasswordStore<AppUser>, IUserPhoneNumberStore<AppUser>, IUserSecurityStampStore<AppUser>, IUserRoleStore<AppUser>
    {
        public RavenUserStore(IAsyncDocumentSession documentSession, ILogger<RavenUserStore> logger)
            : base(new IdentityErrorDescriber())
        {
            if (documentSession == null)
                throw new ArgumentNullException(nameof(documentSession));

            if (logger == null)
                throw new ArgumentNullException(nameof(logger));

            DocumentSession = documentSession;
            Log = logger;
        }

        IAsyncDocumentSession DocumentSession { get; }
        
        ILogger Log { get; }

        public override IQueryable<AppUser> Users => DocumentSession.Query<AppUser>();

        protected override async Task<AppUser> FindUserAsync(string userId, CancellationToken cancellationToken)
        {
            if (String.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("Argument cannot be null, empty, or entirely composed of whitespace: 'userId'.", nameof(userId));

            return await DocumentSession.LoadAsync<AppUser>(userId, cancellationToken);
        }

        public override async Task<AppUser> FindByIdAsync(string userId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("Argument cannot be null, empty, or entirely composed of whitespace: 'userId'.", nameof(userId));

            return await DocumentSession.LoadAsync<AppUser>(userId, cancellationToken);
        }

        public override async Task<AppUser> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(normalizedUserName))
                throw new ArgumentException("Argument cannot be null, empty, or entirely composed of whitespace: 'normalizedUserName'.", nameof(normalizedUserName));
            
            return await DocumentSession.Query<AppUser>().FirstOrDefaultAsync(
                user => user.NormalizedUserName == normalizedUserName
            );
        }

        public override async Task<AppUser> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(normalizedEmail))
                throw new ArgumentException("Argument cannot be null, empty, or entirely composed of whitespace: 'normalizedEmail'.", nameof(normalizedEmail));
            
            return await DocumentSession.Query<AppUser>().FirstOrDefaultAsync(
                user => user.NormalizedEmail == normalizedEmail
            );
        }

        public override async Task<IdentityResult> CreateAsync(AppUser user, CancellationToken cancellationToken = default)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            await DocumentSession.StoreAsync(user, cancellationToken);
            await DocumentSession.SaveChangesAsync(cancellationToken);

            return IdentityResult.Success;
        }

        public override async Task<IdentityResult> UpdateAsync(AppUser user, CancellationToken cancellationToken = default)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            await DocumentSession.StoreAsync(user, cancellationToken);
            await DocumentSession.SaveChangesAsync(cancellationToken);

            return IdentityResult.Success;
        }

        public override async Task<IdentityResult> DeleteAsync(AppUser user, CancellationToken cancellationToken = default)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            DocumentSession.Delete(user);
            await DocumentSession.SaveChangesAsync(cancellationToken);

            return IdentityResult.Success;
        }

        public override Task<IList<Claim>> GetClaimsAsync(AppUser user, CancellationToken cancellationToken = default)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));
            
            return Task.FromResult<IList<Claim>>(
                user.Claims.Select(
                    claim => new Claim(claim.ClaimType, claim.ClaimValue)
                )
                .ToList()
            );
        }

        public override async Task<IList<AppUser>> GetUsersForClaimAsync(Claim claim, CancellationToken cancellationToken = default)
        {
            if (claim == null)
                throw new ArgumentNullException(nameof(claim));
            
            return await DocumentSession.Query<AppUser>()
                .Where(user => user.Claims.Any(
                    userClaim => userClaim.ClaimType == claim.Type && userClaim.ClaimValue == claim.Value
                ))
                .ToListAsync();
        }

        public override Task AddClaimsAsync(AppUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken = default)
        {
            foreach (Claim claim in claims)
            {
                user.Claims.Add(new AppUserClaim
                {
                    UserId = user.Id,
                    ClaimType = claim.Type,
                    ClaimValue = claim.Value
                });
            }

            return Task.CompletedTask;
        }

        public override Task ReplaceClaimAsync(AppUser user, Claim claim, Claim newClaim, CancellationToken cancellationToken = default)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));
            
            if (claim == null)
                throw new ArgumentNullException(nameof(claim));
            
            if (newClaim == null)
                throw new ArgumentNullException(nameof(newClaim));
            
            AppUserClaim existingUserClaim = user.Claims.FirstOrDefault(
                userClaim => userClaim.ClaimType == claim.Type && userClaim.ClaimValue == claim.Value
            );
            if (existingUserClaim != null)
                user.Claims.Remove(existingUserClaim);

            user.Claims.Add(new AppUserClaim
            {
                UserId = user.Id,
                ClaimType = newClaim.Type,
                ClaimValue = newClaim.Value
            });


            return Task.CompletedTask;
        }

        public override Task RemoveClaimsAsync(AppUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken = default)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            if (claims == null)
                throw new ArgumentNullException(nameof(claims));

            foreach (Claim claim in claims)
            {
                AppUserClaim existingUserClaim = user.Claims.FirstOrDefault(
                    userClaim => userClaim.ClaimType == claim.Type && userClaim.ClaimValue == claim.Value
                );
                if (existingUserClaim != null)
                    user.Claims.Remove(existingUserClaim);
            }

            return Task.CompletedTask;
        }

        protected override async Task<AppUserLogin> FindUserLoginAsync(string userId, string loginProvider, string providerKey, CancellationToken cancellationToken)
        {
            if (String.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("Argument cannot be null, empty, or entirely composed of whitespace: 'userId'.", nameof(userId));
            
            if (String.IsNullOrWhiteSpace(loginProvider))
                throw new ArgumentException("Argument cannot be null, empty, or entirely composed of whitespace: 'loginProvider'.", nameof(loginProvider));
            
            if (String.IsNullOrWhiteSpace(providerKey))
                throw new ArgumentException("Argument cannot be null, empty, or entirely composed of whitespace: 'providerKey'.", nameof(providerKey));
            
            AppUser user = await DocumentSession.LoadAsync<AppUser>(userId, cancellationToken);
            if (user == null)
                return null;

            return user.UserLogins.FirstOrDefault(
                login => login.LoginProvider == loginProvider && login.ProviderKey == providerKey
            );
        }

        protected override async Task<AppUserLogin> FindUserLoginAsync(string loginProvider, string providerKey, CancellationToken cancellationToken)
        {
            if (String.IsNullOrWhiteSpace(loginProvider))
                throw new ArgumentException("Argument cannot be null, empty, or entirely composed of whitespace: 'loginProvider'.", nameof(loginProvider));
            
            if (String.IsNullOrWhiteSpace(providerKey))
                throw new ArgumentException("Argument cannot be null, empty, or entirely composed of whitespace: 'providerKey'.", nameof(providerKey));
            
            AppUser userWithLogin = await DocumentSession.Query<AppUser>().FirstOrDefaultAsync(
                user => user.UserLogins.Any(
                    login => login.LoginProvider == loginProvider && login.ProviderKey == providerKey
                )
            );
            if (userWithLogin == null)
                return null;

            return userWithLogin.UserLogins.FirstOrDefault(
                login => login.LoginProvider == loginProvider && login.ProviderKey == providerKey
            );
        }

        public override Task<IList<UserLoginInfo>> GetLoginsAsync(AppUser user, CancellationToken cancellationToken = default)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));
            
            return Task.FromResult<IList<UserLoginInfo>>(
                user.Logins.ToList()
            );
        }

        public override Task AddLoginAsync(AppUser user, UserLoginInfo login, CancellationToken cancellationToken = default)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));
            
            user.Logins.Add(login);
            
            return Task.CompletedTask;
        }

        public override Task RemoveLoginAsync(AppUser user, string loginProvider, string providerKey, CancellationToken cancellationToken = default)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));
            
            UserLoginInfo existingLogin = user.Logins.FirstOrDefault(
                login => login.LoginProvider == loginProvider && login.ProviderKey == providerKey
            );
            if (existingLogin != null)
                user.Logins.Remove(existingLogin);

            return Task.CompletedTask;
        }

        protected override Task<AppUserToken> FindTokenAsync(AppUser user, string loginProvider, string name, CancellationToken cancellationToken)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));
            
            return Task.FromResult(
                user.Tokens.FirstOrDefault(token => token.LoginProvider == loginProvider && token.Name == name)
            );
        }

        protected override async Task AddUserTokenAsync(AppUserToken token)
        {
            if (token == null)
                throw new ArgumentNullException(nameof(token));
            
            AppUser user = await DocumentSession.LoadAsync<AppUser>(token.UserId);
            if (user == null)
                throw new InvalidCastException($"User not found with Id '{token.UserId}'.");

            user.Tokens.Add(token);
        }

        protected override async Task RemoveUserTokenAsync(AppUserToken token)
        {
            if (token == null)
                throw new ArgumentNullException(nameof(token));
            
            AppUser user = await DocumentSession.LoadAsync<AppUser>(token.UserId);
            if (user == null)
                throw new InvalidCastException($"User not found with Id '{token.UserId}'.");

            AppUserToken existingToken = user.Tokens.FirstOrDefault(
                userToken => userToken.LoginProvider == token.LoginProvider && userToken.Name == token.Name
            );
            if (existingToken != null)
                user.Tokens.Remove(existingToken);
        }

        public async Task AddToRoleAsync(AppUser user, string roleName, CancellationToken cancellationToken)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            if (user.Roles.Any(userRole => userRole.NormalizedRoleName == roleName))
                return;

            AppRole targetRole = await DocumentSession.Query<AppRole>().FirstOrDefaultAsync(
                role => role.NormalizedName == roleName
            );
            if (targetRole == null)
                throw new InvalidOperationException($"AppRole not found with name '{roleName}'.");

            user.Roles.Add(new AppUserRole
            {
                RoleId = targetRole.Id,
                RoleName = targetRole.Name,
                NormalizedRoleName = targetRole.NormalizedName
            });
        }

        public Task RemoveFromRoleAsync(AppUser user, string roleName, CancellationToken cancellationToken)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));
            
            AppUserRole targetUserRole = user.Roles.FirstOrDefault(
                userRole => userRole.NormalizedRoleName == roleName
            );
            if (targetUserRole == null)
                return Task.CompletedTask;

            user.Roles.Remove(targetUserRole);
            
            return Task.CompletedTask;
        }

        public Task<IList<string>> GetRolesAsync(AppUser user, CancellationToken cancellationToken)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            return Task.FromResult<IList<string>>(
                user.Roles.Select(
                    userRole => userRole.RoleId
                )
                .ToList()
            );
        }

        public Task<bool> IsInRoleAsync(AppUser user, string roleName, CancellationToken cancellationToken)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));
            
            return Task.FromResult(
                user.Roles.Any(userRole => userRole.NormalizedRoleName == roleName)
            );
        }

        public async Task<IList<AppUser>> GetUsersInRoleAsync(string roleName, CancellationToken cancellationToken)
        {
            return await DocumentSession.Query<AppUser>()
                .Where(user => user.Roles.Any(userRole => userRole.NormalizedRoleName == roleName))
                .ToListAsync(cancellationToken);
        }
    }
}
