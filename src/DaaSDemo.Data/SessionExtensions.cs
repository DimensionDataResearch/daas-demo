using Raven.Client.Documents.Session;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DaaSDemo.Data
{
    using Models.Data;

    /// <summary>
    ///     <see cref="IDocumentSession"> extension methods for data-access.
    /// </summary>
    public static class SessionExtensions
    {
        /// <summary>
        ///     Get a tenant by Id.
        /// </summary>
        /// <param name="session">
        ///     The RavenDB document session.
        /// </param>
        /// <param name="tenantId">
        ///     The tenant Id.
        /// </param>
        /// <returns>
        ///     The <see cref="Tenant"/>, or <c>null</c> if no tenant was found with the specified Id.
        /// </returns>
        public static Tenant GetTenantById(this IDocumentSession session, string tenantId) => session.Load<Tenant>(tenantId);

        /// <summary>
        ///     Get all tenants, ordered by name.
        /// </summary>
        /// <param name="session">
        ///     The RavenDB document session.
        /// </param>
        /// <returns>
        ///     The <see cref="Tenant"/>s.
        /// </returns>
        public static Tenant[] GetAllTenants(this IDocumentSession session) => session.Query<Tenant>().OrderBy(tenant => tenant.Name).ToArray();

        /// <summary>
        ///     Get a database server by Id.
        /// </summary>
        /// <param name="session">
        ///     The RavenDB document session.
        /// </param>
        /// <param name="serverId">
        ///     The database server Id.
        /// </param>
        /// <returns>
        ///     The <see cref="DatabaseServer"/>, or <c>null</c> if no database server was found with the specified Id.
        /// </returns>
        public static DatabaseServer GetDatabaseServerById(this IDocumentSession session, string serverId) => session.Load<DatabaseServer>(serverId);

        /// <summary>
        ///     Get a database server by tenant Id.
        /// </summary>
        /// <param name="session">
        ///     The RavenDB document session.
        /// </param>
        /// <param name="tenantId">
        ///     The Id of the tenant that owns the database server.
        /// </param>
        /// <returns>
        ///     The <see cref="DatabaseServer"/>, or <c>null</c> if no database server was found with the specified tenant Id.
        /// </returns>
        public static DatabaseServer GetDatabaseServerByTenantId(this IDocumentSession session, string tenantId) => session.Query<DatabaseServer>().FirstOrDefault(server => server.TenantId == tenantId);

        /// <summary>
        ///     Determine if the specified server has any databases.
        /// </summary>
        /// <param name="session">
        ///     The RavenDB document session.
        /// </param>
        /// <param name="serverId">
        ///     The server Id.
        /// </param>
        /// <returns>
        ///     <c>true</c> if the server has any databases; otherwise, <c>false</c>.
        /// </returns>
        public static bool DoesServerHaveDatabases(this IDocumentSession session, string serverId) => session.Query<DatabaseInstance>().Any(database => database.ServerId == serverId);

        /// <summary>
        ///     Get all databases, ordered by server Id, then name.
        /// </summary>
        /// <param name="session">
        ///     The RavenDB document session.
        /// </param>
        /// <returns>
        ///     The <see cref="DatabaseInstance"/>s.
        /// </returns>
        public static DatabaseInstance[] GetAllDatabases(this IDocumentSession session) => session.Query<DatabaseInstance>().OrderBy(database => database.ServerId).ThenBy(database => database.Name).ToArray();

        /// <summary>
        ///     Get all databases for the specified server, ordered by name.
        /// </summary>
        /// <param name="session">
        ///     The RavenDB document session.
        /// </param>
        /// <param name="serverId">
        ///     The server Id.
        /// </param>
        /// <returns>
        ///     The <see cref="DatabaseInstance"/>s.
        /// </returns>
        public static DatabaseInstance[] GetServerDatabases(this IDocumentSession session, string serverId) => session.Query<DatabaseInstance>().Where(database => database.ServerId == serverId).OrderBy(database => database.Name).ToArray();

        /// <summary>
        ///     Get a database instance by Id.
        /// </summary>
        /// <param name="session">
        ///     The RavenDB document session.
        /// </param>
        /// <param name="databaseId">
        ///     The database instance Id.
        /// </param>
        /// <returns>
        ///     The <see cref="DatabaseInstance"/>, or <c>null</c> if no database instance was found with the specified Id.
        /// </returns>
        public static DatabaseInstance GetDatabaseById(this IDocumentSession session, string databaseId) => session.Load<DatabaseInstance>(databaseId);

        /// <summary>
        ///     Get a database instance by name and server Id.
        /// </summary>
        /// <param name="session">
        ///     The RavenDB document session.
        /// </param>
        /// <param name="name">
        ///     The name of the database to find.
        /// </param>
        /// <param name="serverId">
        ///     The Id of the server that hosts the database.
        /// </param>
        /// <returns>
        ///     The <see cref="DatabaseInstance"/>, or <c>null</c> if no database instance was found with the specified Id.
        /// </returns>
        public static DatabaseInstance GetDatabaseInstanceByName(this IDocumentSession session, string name, string serverId) => session.Query<DatabaseInstance>().FirstOrDefault(database => database.Name == name && database.ServerId == serverId);

        /// <summary>
        ///     Get all <see cref="DatabaseInstance"/>s in the specified database server.
        /// </summary>
        /// <param name="session">
        ///     The RavenDB document session.
        /// </param>
        /// <param name="serverId">
        ///     The database server Id.
        /// </param>
        /// <returns>
        ///     A sequence of <see cref="DatabaseInstance"/>s, sorted by name.
        /// </returns>
        public static IEnumerable<DatabaseInstance> GetDatabaseInstancesByServer(this IDocumentSession session, string serverId)
        {
            return
                session.Query<DatabaseInstance>().Where(
                    database => database.ServerId == serverId
                )
                .OrderBy(database => database.Name)
                .AsEnumerable();
        }
    }
}
