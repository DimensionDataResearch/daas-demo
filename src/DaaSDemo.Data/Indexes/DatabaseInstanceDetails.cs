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

                    StorageMB = database.Storage.SizeMB,
                    
                    Action = database.Action,
                    Status = database.Status,

                    ServerId = database.ServerId,
                    ServerName = server.Name,

                    TenantId = database.TenantId,
                    TenantName = tenant.Name,

                    ConnectionString = (server.PublicPort != null) ? $"Data Source=tcp:{server.PublicFQDN}:{server.PublicPort};Initial Catalog={database.Name};User={database.DatabaseUser};Password=<password>" : null
                };

            StoreAllFields(FieldStorage.Yes);
        }
    }
}
