using Microsoft.AspNetCore.Identity;
using Raven.Client.Documents;
using Raven.Client.Documents.Session;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DaaSDemo.Identity.Stores
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Claims;
    using Models.Data;

    // TODO: Tidy this up.

    public class RavenRoleStore
        : RoleStoreBase<AppRole, string, IdentityUserRole<string>, AppRoleClaim>, IRoleStore<AppRole>
    {
        public RavenRoleStore(IAsyncDocumentSession documentSession)
            : base(new IdentityErrorDescriber())
        {
            if (documentSession == null)
                throw new ArgumentNullException(nameof(documentSession));
            
            DocumentSession = documentSession;
        }

        IAsyncDocumentSession DocumentSession { get; }

        public override IQueryable<AppRole> Roles => DocumentSession.Query<AppRole>();

        public override async Task<IdentityResult> CreateAsync(AppRole role, CancellationToken cancellationToken = default)
        {
            if (role == null)
                throw new ArgumentNullException(nameof(role));

            await DocumentSession.StoreAsync(role, cancellationToken);
            await DocumentSession.SaveChangesAsync(cancellationToken);

            return IdentityResult.Success;
        }

        public override async Task<IdentityResult> DeleteAsync(AppRole role, CancellationToken cancellationToken = default)
        {
            DocumentSession.Delete(role);
            await DocumentSession.SaveChangesAsync(cancellationToken);

            return IdentityResult.Success;
        }

        public override async Task<AppRole> FindByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Argument cannot be null, empty, or entirely composed of whitespace: 'id'.", nameof(id));

            return await DocumentSession.LoadAsync<AppRole>(id);
        }

        public override async Task<AppRole> FindByNameAsync(string normalizedName, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(normalizedName))
                throw new ArgumentException("Argument cannot be null, empty, or entirely composed of whitespace: 'normalizedName'.", nameof(normalizedName));

            return await DocumentSession.Query<AppRole>().FirstOrDefaultAsync(
                role => role.Name == normalizedName
            );
        }

        public override Task<IList<Claim>> GetClaimsAsync(AppRole role, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IList<Claim>>(
                role.Claims
                    .Select(
                        claim => new Claim(claim.ClaimType, claim.ClaimValue)
                    )
                    .ToList()
            );
        }

        public override Task AddClaimAsync(AppRole role, Claim claim, CancellationToken cancellationToken = default)
        {
            role.Claims.Add(new AppRoleClaim
            {
                ClaimType = claim.Type,
                ClaimValue = claim.Value
            });

            return Task.CompletedTask;
        }

        public override Task RemoveClaimAsync(AppRole role, Claim claim, CancellationToken cancellationToken = default)
        {
            if (role == null)
                throw new ArgumentNullException(nameof(role));
            
            if (claim == null)
                throw new ArgumentNullException(nameof(claim));
            
            AppRoleClaim existingRoleClaim = role.Claims.FirstOrDefault(
                roleClaim => roleClaim.ClaimType == claim.Type && roleClaim.ClaimValue == claim.Value
            );
            if (existingRoleClaim != null)
                role.Claims.Remove(existingRoleClaim);
            
            return Task.CompletedTask;
        }

        public override async Task<IdentityResult> UpdateAsync(AppRole role, CancellationToken cancellationToken = default)
        {
            if (role == null)
                throw new ArgumentNullException(nameof(role));
            
            if (DocumentSession.Advanced.HasChanged(role))
            {
                role.ConcurrencyStamp = Guid.NewGuid().ToString("N");
                await DocumentSession.StoreAsync(role, cancellationToken);

                await DocumentSession.SaveChangesAsync(cancellationToken);
            }

            return IdentityResult.Success;
        }
    }
}
