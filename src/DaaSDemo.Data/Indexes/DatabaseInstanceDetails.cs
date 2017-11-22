using Raven.Client.Documents.Indexes;
using System.Linq;

namespace DaaSDemo.Data.Indexes
{
    using Models.Api;
    using Models.Data;

    public class DatabaseInstanceDetails
        : AbstractIndexCreationTask<DatabaseInstance, DatabaseInstanceDetail>
    {
        public DatabaseInstanceDetails()
        {
            Map = databases =>
                from database in databases
                let tenant = LoadDocument<Tenant>(database.TenantId)
                let server = LoadDocument<DatabaseServer>(database.ServerId)
                select new DatabaseInstanceDetail
                {
                    Id = server.Id,
                    Name = server.Name,
                    
                    Action = server.Action,
                    Status = server.Status,

                    ServerId = database.ServerId,
                    ServerName = server.Name,

                    TenantId = database.TenantId,
                    TenantName = tenant.Name
                };
        }
    }
}
