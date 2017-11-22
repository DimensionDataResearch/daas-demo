using Raven.Client.Documents.Indexes;
using System.Linq;

namespace DaaSDemo.Data.Indexes
{
    using Models.Api;
    using Models.Data;

    /// <summary>
    ///     Index used to aggregate database server details for use in the DaaS API.
    /// </summary>
    public class DatabaseServerDetails
        : AbstractIndexCreationTask<DatabaseServer, DatabaseServerDetail>
    {
        /// <summary>
        ///     Create a new <see cref="DatabaseServerDetails"/> index definition.
        /// </summary>
        public DatabaseServerDetails()
        {
            Map = servers =>
                from server in servers
                let tenant = LoadDocument<Tenant>(server.TenantId)
                select new DatabaseServerDetail
                {
                    Id = server.Id,
                    Name = server.Name,
                    Kind = server.Kind,

                    StorageMB = server.Storage.SizeMB,

                    PublicFQDN = server.PublicFQDN,
                    PublicPort = server.PublicPort,

                    Action = server.Action,
                    Phase = server.Phase,
                    Status = server.Status,
                    
                    TenantId = server.TenantId,
                    TenantName = tenant.Name
                };

            StoreAllFields(FieldStorage.Yes);
        }
    }
}
