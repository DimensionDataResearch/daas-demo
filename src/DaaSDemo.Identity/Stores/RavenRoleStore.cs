using Microsoft.AspNetCore.Identity;
using Raven.Client.Documents;
using Raven.Client.Documents.Session;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DaaSDemo.Identity.Stores
{
    using Models.Data;

    // TODO: Tidy this up.

    public class RavenRoleStore
        : IRoleStore<AppRole>
    {
        public RavenRoleStore(IDocumentStore documentStore)
        {
            if (documentStore == null)
                throw new ArgumentNullException(nameof(documentStore));
            
            DocumentStore = documentStore;
        }

        IDocumentStore DocumentStore { get; }

        public async Task<IdentityResult> CreateAsync(AppRole role, CancellationToken cancellationToken)
        {
            if (role == null)
                throw new ArgumentNullException(nameof(role));
            
            using (IAsyncDocumentSession session = DocumentStore.OpenAsyncSession())
            {
                await session.StoreAsync(role, cancellationToken);
                
                await session.SaveChangesAsync(cancellationToken);
            }

            return IdentityResult.Success;
        }

        public async Task<IdentityResult> UpdateAsync(AppRole role, CancellationToken cancellationToken)
        {
            using (IAsyncDocumentSession session = DocumentStore.OpenAsyncSession())
            {
                await session.StoreAsync(role, cancellationToken);
                
                await session.SaveChangesAsync(cancellationToken);
            }

            return IdentityResult.Success;
        }

        public async Task<IdentityResult> DeleteAsync(AppRole role, CancellationToken cancellationToken)
        {
            if (role == null)
                throw new ArgumentNullException(nameof(role));
            
            using (IAsyncDocumentSession session = DocumentStore.OpenAsyncSession())
            {
                session.Delete(role.Id);
                
                await session.SaveChangesAsync(cancellationToken);
            }

            return IdentityResult.Success;
        }

        public async Task<AppRole> FindByIdAsync(string roleId, CancellationToken cancellationToken)
        {
            if (String.IsNullOrWhiteSpace(roleId))
                throw new ArgumentException("Argument cannot be null, empty, or entirely composed of whitespace: 'roleId'.", nameof(roleId));
            
            using (IAsyncDocumentSession session = DocumentStore.OpenAsyncSession())
            {
                return await session.LoadAsync<AppRole>(roleId);
            }
        }

        public async Task<AppRole> FindByNameAsync(string normalizedRoleName, CancellationToken cancellationToken)
        {
            if (String.IsNullOrWhiteSpace(normalizedRoleName))
                throw new ArgumentException("Argument cannot be null, empty, or entirely composed of whitespace: 'normalizedRoleName'.", nameof(normalizedRoleName));
            
            using (IAsyncDocumentSession session = DocumentStore.OpenAsyncSession())
            {
                return await session.Query<AppRole>().FirstOrDefaultAsync(
                    role => role.Name == normalizedRoleName
                );
            }
        }
        
        public Task<string> GetRoleIdAsync(AppRole role, CancellationToken cancellationToken) => Task.FromResult(role.Id);

        public Task<string> GetNormalizedRoleNameAsync(AppRole role, CancellationToken cancellationToken) => Task.FromResult(role.Name);
        public Task SetNormalizedRoleNameAsync(AppRole role, string normalizedName, CancellationToken cancellationToken)
        {
            role.Name = normalizedName;

            return Task.CompletedTask;
        }


        public Task<string> GetRoleNameAsync(AppRole role, CancellationToken cancellationToken) => Task.FromResult(role.DisplayName);
        public Task SetRoleNameAsync(AppRole role, string roleName, CancellationToken cancellationToken)
        {
            role.DisplayName = roleName;

            return Task.CompletedTask;
        }

        public void Dispose()
        {
        }
    }
}
