using Raven.Client.Documents.Indexes;
using System.Linq;

namespace DaaSDemo.Data.Indexes
{
    using Models.Api;
    using Models.Data;

    public class DatabaseServerDetails
        : AbstractIndexCreationTask<DatabaseServer, DatabaseServerDetail>
    {
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

                    PublicFQDN = server.PublicFQDN,
                    PublicPort = server.PublicPort,

                    Action = server.Action,
                    Phase = server.Phase,
                    Status = server.Status,
                    
                    TenantId = server.TenantId,
                    TenantName = tenant.Name
                };
        }
    }
}
