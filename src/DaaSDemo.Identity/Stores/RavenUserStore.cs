using Microsoft.AspNetCore.Identity;
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

    // TODO: Tidy this up.

    public class RavenUserStore
        : IUserStore<AppUser>, IUserRoleStore<AppUser>, IUserPasswordStore<AppUser>, IUserSecurityStampStore<AppUser>, IUserEmailStore<AppUser>, IUserLockoutStore<AppUser>, IUserClaimStore<AppUser>
    {
        public RavenUserStore(IDocumentStore documentStore)
        {
            if (documentStore == null)
                throw new ArgumentNullException(nameof(documentStore));
            
            DocumentStore = documentStore;
        }
        
        IDocumentStore DocumentStore { get; }

        public async Task<IdentityResult> CreateAsync(AppUser user, CancellationToken cancellationToken)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));
            
            using (IAsyncDocumentSession session = DocumentStore.OpenAsyncSession())
            {
                await session.StoreAsync(user, cancellationToken);
                
                await session.SaveChangesAsync(cancellationToken);
            }

            return IdentityResult.Success;
        }

        public async Task<IdentityResult> UpdateAsync(AppUser user, CancellationToken cancellationToken)
        {
            using (IAsyncDocumentSession session = DocumentStore.OpenAsyncSession())
            {
                await session.StoreAsync(user, cancellationToken);
                
                await session.SaveChangesAsync(cancellationToken);
            }

            return IdentityResult.Success;
        }

        public async Task<IdentityResult> DeleteAsync(AppUser user, CancellationToken cancellationToken)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));
            
            using (IAsyncDocumentSession session = DocumentStore.OpenAsyncSession())
            {
                session.Delete(user.Id);
                
                await session.SaveChangesAsync(cancellationToken);
            }

            return IdentityResult.Success;
        }

        public async Task<AppUser> FindByIdAsync(string userId, CancellationToken cancellationToken)
        {
            if (String.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("Argument cannot be null, empty, or entirely composed of whitespace: 'userId'.", nameof(userId));
            
            using (IAsyncDocumentSession session = DocumentStore.OpenAsyncSession())
            {
                return await session.LoadAsync<AppUser>(userId, cancellationToken);
            }
        }

        public async Task<AppUser> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
        {
            if (String.IsNullOrWhiteSpace(normalizedUserName))
                throw new ArgumentException("Argument cannot be null, empty, or entirely composed of whitespace: 'normalizedUserName'.", nameof(normalizedUserName));
            
            using (IAsyncDocumentSession session = DocumentStore.OpenAsyncSession())
            {
                return await session.Query<AppUser>().FirstOrDefaultAsync(
                    user => user.Name == normalizedUserName,
                    cancellationToken
                );
            }
        }
        
        public Task<string> GetUserIdAsync(AppUser user, CancellationToken cancellationToken) => Task.FromResult(user.Id);

        public Task<string> GetNormalizedUserNameAsync(AppUser user, CancellationToken cancellationToken) => Task.FromResult(user.Name);
        public Task SetNormalizedUserNameAsync(AppUser user, string normalizedName, CancellationToken cancellationToken)
        {
            user.Name = normalizedName;

            return Task.CompletedTask;
        }


        public Task<string> GetUserNameAsync(AppUser user, CancellationToken cancellationToken) => Task.FromResult(user.DisplayName);
        public Task SetUserNameAsync(AppUser user, string userName, CancellationToken cancellationToken)
        {
            user.DisplayName = userName;

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            // AF: Research the life-cycle of store components to see if their are typically resolved from components that are, themselves, scoped.
        }

        public async Task AddToRoleAsync(AppUser user, string roleName, CancellationToken cancellationToken)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));
                
            using (IAsyncDocumentSession session = DocumentStore.OpenAsyncSession())
            {
                AppUser targetUser = await session.LoadAsync<AppUser>(user.Id, cancellationToken);
                if (targetUser == null)
                    throw new InvalidOperationException($"User not found with Id '{user.Id}'.");

                AppRole targetRole = await session.Query<AppRole>().FirstOrDefaultAsync(
                    role => role.Name == roleName,
                    cancellationToken
                );

                // TODO: Throw if null.

                targetUser.RoleIds.Add(targetRole.Id);
                
                await session.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task RemoveFromRoleAsync(AppUser user, string roleName, CancellationToken cancellationToken)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            using (IAsyncDocumentSession session = DocumentStore.OpenAsyncSession())
            {
                AppUser targetUser = await session.LoadAsync<AppUser>(user.Id, cancellationToken);
                if (targetUser == null)
                    throw new InvalidOperationException($"User not found with Id '{user.Id}'.");

                AppRole targetRole = await session.Query<AppRole>().FirstOrDefaultAsync(
                    role => role.Name == roleName,
                    cancellationToken
                );

                // TODO: Throw if null.

                targetUser.RoleIds.Remove(targetRole.Id);
                
                await session.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task<IList<string>> GetRolesAsync(AppUser user, CancellationToken cancellationToken)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            using (IAsyncDocumentSession session = DocumentStore.OpenAsyncSession())
            {
                AppUser targetUser = await session.LoadAsync<AppUser>(user.Id, cancellationToken);
                if (targetUser == null)
                    throw new InvalidOperationException($"User not found with Id '{user.Id}'.");

                return targetUser.RoleIds.ToList();
            }
        }

        public async Task<bool> IsInRoleAsync(AppUser user, string roleName, CancellationToken cancellationToken)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            using (IAsyncDocumentSession session = DocumentStore.OpenAsyncSession())
            {
                AppUser targetUser = await session.LoadAsync<AppUser>(user.Id, cancellationToken);
                if (targetUser == null)
                    throw new InvalidOperationException($"User not found with Id '{user.Id}'.");

                AppRole targetRole = await session.Query<AppRole>().FirstOrDefaultAsync(
                    role => role.Name == roleName,
                    cancellationToken
                );

                // TODO: Throw if null.

                return targetUser.RoleIds.Contains(targetRole.Id);
            }
        }

        public async Task<IList<AppUser>> GetUsersInRoleAsync(string roleName, CancellationToken cancellationToken)
        {
            using (IAsyncDocumentSession session = DocumentStore.OpenAsyncSession())
            {
                AppRole targetRole = await session.Query<AppRole>().FirstOrDefaultAsync(
                    role => role.Name == roleName,
                    cancellationToken
                );

                // TODO: Throw if null.

                return await session.Query<AppUser>()
                    .Where(
                        user => user.RoleIds.Contains(targetRole.Id)
                    )
                    .ToListAsync(cancellationToken);
            }
        }

        public async Task<bool> HasPasswordAsync(AppUser user, CancellationToken cancellationToken)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            using (IAsyncDocumentSession session = DocumentStore.OpenAsyncSession())
            {
                AppUser targetUser = await session.LoadAsync<AppUser>(user.Id, cancellationToken);
                    if (targetUser == null)
                        throw new InvalidOperationException($"User not found with Id '{user.Id}'.");

                return !String.IsNullOrWhiteSpace(user.PasswordHash);
            }
        }

        public async Task<string> GetPasswordHashAsync(AppUser user, CancellationToken cancellationToken)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            using (IAsyncDocumentSession session = DocumentStore.OpenAsyncSession())
            {
                AppUser targetUser = await session.LoadAsync<AppUser>(user.Id, cancellationToken);
                    if (targetUser == null)
                        throw new InvalidOperationException($"User not found with Id '{user.Id}'.");

                return user.PasswordHash;
            }
        }

        public async Task SetPasswordHashAsync(AppUser user, string passwordHash, CancellationToken cancellationToken)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            using (IAsyncDocumentSession session = DocumentStore.OpenAsyncSession())
            {
                AppUser targetUser = await session.LoadAsync<AppUser>(user.Id, cancellationToken);
                if (targetUser == null)
                    throw new InvalidOperationException($"User not found with Id '{user.Id}'.");

                targetUser.PasswordHash = passwordHash;

                await session.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task SetSecurityStampAsync(AppUser user, string stamp, CancellationToken cancellationToken)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));
            
            using (IAsyncDocumentSession session = DocumentStore.OpenAsyncSession())
            {
                AppUser targetUser = await session.LoadAsync<AppUser>(user.Id, cancellationToken);
                if (targetUser == null)
                    throw new InvalidOperationException($"User not found with Id '{user.Id}'.");

                targetUser.SecurityStamp = stamp;

                await session.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task<string> GetSecurityStampAsync(AppUser user, CancellationToken cancellationToken)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));
            
            using (IAsyncDocumentSession session = DocumentStore.OpenAsyncSession())
            {
                AppUser targetUser = await session.LoadAsync<AppUser>(user.Id, cancellationToken);
                if (targetUser == null)
                    throw new InvalidOperationException($"User not found with Id '{user.Id}'.");

                return targetUser.SecurityStamp;
            }
        }

        public async Task<AppUser> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
        {
            using (IAsyncDocumentSession session = DocumentStore.OpenAsyncSession())
            {
                return await session.Query<AppUser>()
                    .FirstOrDefaultAsync(
                        user => user.EmailAddress == normalizedEmail,
                        cancellationToken
                    );
            }
        }

        public async Task<string> GetEmailAsync(AppUser user, CancellationToken cancellationToken)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));
            
            using (IAsyncDocumentSession session = DocumentStore.OpenAsyncSession())
            {
                AppUser targetUser = await session.LoadAsync<AppUser>(user.Id, cancellationToken);
                if (targetUser == null)
                    throw new InvalidOperationException($"User not found with Id '{user.Id}'.");

                return targetUser.EmailAddress;
            }
        }

        public async Task SetEmailAsync(AppUser user, string email, CancellationToken cancellationToken)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));
            
            using (IAsyncDocumentSession session = DocumentStore.OpenAsyncSession())
            {
                AppUser targetUser = await session.LoadAsync<AppUser>(user.Id, cancellationToken);
                if (targetUser == null)
                    throw new InvalidOperationException($"User not found with Id '{user.Id}'.");

                targetUser.EmailAddress = email;

                await session.SaveChangesAsync(cancellationToken);
            }
        }

        public Task<string> GetNormalizedEmailAsync(AppUser user, CancellationToken cancellationToken) => GetEmailAsync(user, cancellationToken);

        public Task SetNormalizedEmailAsync(AppUser user, string normalizedEmail, CancellationToken cancellationToken) => SetEmailAsync(user, normalizedEmail, cancellationToken);

        public async Task<bool> GetEmailConfirmedAsync(AppUser user, CancellationToken cancellationToken)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));
            
            using (IAsyncDocumentSession session = DocumentStore.OpenAsyncSession())
            {
                AppUser targetUser = await session.LoadAsync<AppUser>(user.Id, cancellationToken);
                if (targetUser == null)
                    throw new InvalidOperationException($"User not found with Id '{user.Id}'.");

                return targetUser.IsEmailAddressConfirmed;
            }
        }

        public async Task SetEmailConfirmedAsync(AppUser user, bool confirmed, CancellationToken cancellationToken)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));
            
            using (IAsyncDocumentSession session = DocumentStore.OpenAsyncSession())
            {
                AppUser targetUser = await session.LoadAsync<AppUser>(user.Id, cancellationToken);
                if (targetUser == null)
                    throw new InvalidOperationException($"User not found with Id '{user.Id}'.");

                targetUser.IsEmailAddressConfirmed = confirmed;

                await session.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task<DateTimeOffset?> GetLockoutEndDateAsync(AppUser user, CancellationToken cancellationToken)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));
            
            using (IAsyncDocumentSession session = DocumentStore.OpenAsyncSession())
            {
                AppUser targetUser = await session.LoadAsync<AppUser>(user.Id, cancellationToken);
                if (targetUser == null)
                    throw new InvalidOperationException($"User not found with Id '{user.Id}'.");

                return targetUser.Lockout.EndDate;
            }
        }

        public async Task SetLockoutEndDateAsync(AppUser user, DateTimeOffset? lockoutEnd, CancellationToken cancellationToken)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));
            
            using (IAsyncDocumentSession session = DocumentStore.OpenAsyncSession())
            {
                AppUser targetUser = await session.LoadAsync<AppUser>(user.Id, cancellationToken);
                if (targetUser == null)
                    throw new InvalidOperationException($"User not found with Id '{user.Id}'.");

                targetUser.Lockout.EndDate = lockoutEnd;
            }
        }

        public async Task<int> GetAccessFailedCountAsync(AppUser user, CancellationToken cancellationToken)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));
            
            using (IAsyncDocumentSession session = DocumentStore.OpenAsyncSession())
            {
                AppUser targetUser = await session.LoadAsync<AppUser>(user.Id, cancellationToken);
                if (targetUser == null)
                    throw new InvalidOperationException($"User not found with Id '{user.Id}'.");

                return targetUser.Lockout.AccessFailedCount;
            }
        }

        public async Task<int> IncrementAccessFailedCountAsync(AppUser user, CancellationToken cancellationToken)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));
            
            using (IAsyncDocumentSession session = DocumentStore.OpenAsyncSession())
            {
                AppUser targetUser = await session.LoadAsync<AppUser>(user.Id, cancellationToken);
                if (targetUser == null)
                    throw new InvalidOperationException($"User not found with Id '{user.Id}'.");

                targetUser.Lockout.AccessFailedCount++;

                await session.SaveChangesAsync(cancellationToken);

                return targetUser.Lockout.AccessFailedCount;
            }
        }

        public async Task ResetAccessFailedCountAsync(AppUser user, CancellationToken cancellationToken)
        {
            using (IAsyncDocumentSession session = DocumentStore.OpenAsyncSession())
            {
                AppUser targetUser = await session.LoadAsync<AppUser>(user.Id, cancellationToken);
                if (targetUser == null)
                    throw new InvalidOperationException($"User not found with Id '{user.Id}'.");

                targetUser.Lockout.AccessFailedCount = 0;

                await session.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task<bool> GetLockoutEnabledAsync(AppUser user, CancellationToken cancellationToken)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));
            
            using (IAsyncDocumentSession session = DocumentStore.OpenAsyncSession())
            {
                AppUser targetUser = await session.LoadAsync<AppUser>(user.Id, cancellationToken);
                if (targetUser == null)
                    throw new InvalidOperationException($"User not found with Id '{user.Id}'.");

                targetUser.Lockout.AccessFailedCount++;

                await session.SaveChangesAsync(cancellationToken);

                return targetUser.Lockout.IsEnabled;
            }
        }

        public async Task SetLockoutEnabledAsync(AppUser user, bool enabled, CancellationToken cancellationToken)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));
            
            using (IAsyncDocumentSession session = DocumentStore.OpenAsyncSession())
            {
                AppUser targetUser = await session.LoadAsync<AppUser>(user.Id, cancellationToken);
                if (targetUser == null)
                    throw new InvalidOperationException($"User not found with Id '{user.Id}'.");

                targetUser.Lockout.IsEnabled = enabled;

                await session.SaveChangesAsync(cancellationToken);
            }
        }

        public Task<IList<Claim>> GetClaimsAsync(AppUser user, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task AddClaimsAsync(AppUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task ReplaceClaimAsync(AppUser user, Claim claim, Claim newClaim, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task RemoveClaimsAsync(AppUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<IList<AppUser>> GetUsersForClaimAsync(Claim claim, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
