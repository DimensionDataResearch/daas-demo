using Raven.Client.Documents.Indexes;
using System.Linq;

namespace DaaSDemo.Data.Indexes
{
    using Models.Api;
    using Models.Data;

    /// <summary>
    ///     Index used to aggregate database details for use in the DaaS API.
    /// </summary>
    public class DatabaseInstanceDetails
        : AbstractIndexCreationTask<DatabaseInstance, DatabaseInstanceDetail>
    {
        /// <summary>
        ///     Create a new <see cref="DatabaseInstanceDetails"/> index definition.
        /// </summary>
        public DatabaseInstanceDetails()
        {
            Map = databases =>
                from database in databases
                let tenant = LoadDocument<Tenant>(database.TenantId)
                let server = LoadDocument<DatabaseServer>(database.ServerId)
                select new DatabaseInstanceDetail
                {
                    Id = database.Id,
                    Name = database.Name,
                    DatabaseUser = database.DatabaseUser,

                    StorageMB = database.Storage.SizeMB,
                    
                    Action = database.Action,
                    Status = database.Status,

                    ServerId = database.ServerId,
                    ServerName = server.Name,
                    ServerKind = server.Kind,
                    ServerPublicFQDN = server.PublicFQDN,
                    ServerPublicPort = server.PublicPort,

                    TenantId = database.TenantId,
                    TenantName = tenant.Name
                };

            StoreAllFields(FieldStorage.Yes);
        }
    }
}
