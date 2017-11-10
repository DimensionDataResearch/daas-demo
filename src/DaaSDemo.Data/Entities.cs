using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace DaaSDemo.Data
{
    using System.Collections.Generic;
    using Models.Data;

    /// <summary>
    ///     Database context for entities used by the Database-as-a-Service demo.
    /// </summary>
    public class Entities
        : DbContext
    {
        /// <summary>
        ///     Create a new DaaS entity database context.
        /// </summary>
        public Entities()
        {
        }

        /// <summary>
        ///     Create a new DaaS entity database context.
        /// </summary>
        /// <param name="options">
        ///     The database context options.
        /// </param>
        public Entities(DbContextOptions options)
            : base(options)
        {
        }

        /// <summary>
        ///     The set of known tenants.
        /// </summary>
        public DbSet<Tenant> Tenants { get; set; }

        /// <summary>
        ///     The set of known database servers.
        /// </summary>
        public DbSet<DatabaseServer> DatabaseServers { get; set; }

        /// <summary>
        ///     The set of known database instances.
        /// </summary>
        public DbSet<DatabaseInstance> DatabaseInstances { get; set; }

        /// <summary>
        ///     Get a tenant by Id.
        /// </summary>
        /// <param name="tenantId">
        ///     The tenant Id.
        /// </param>
        /// <returns>
        ///     The <see cref="Tenant"/>, or <c>null</c> if no tenant was found with the specified Id.
        /// </returns>
        public Tenant GetTenantById(int tenantId) => Tenants.FirstOrDefault(tenant => tenant.Id == tenantId);

        /// <summary>
        ///     Get all tenants, ordered by name.
        /// </summary>
        /// <returns>
        ///     The <see cref="Tenant"/>s.
        /// </returns>
        public Tenant[] GetAllTenants() => Tenants.OrderBy(tenant => tenant.Name).ToArray();

        /// <summary>
        ///     Add a new <see cref="Tenant"/>.
        /// </summary>
        /// <param name="name">
        ///     The tenant name.
        /// </param>
        /// <returns>
        ///     The new tenant.
        /// </returns>
        public Tenant AddTenant(string name)
        {
            if (String.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Argument cannot be null, empty, or entirely composed of whitespace: 'name'.", nameof(name));
            
            var tenant = new Tenant
            {
                Name = name
            };
            Tenants.Add(tenant);

            return tenant;
        }

        /// <summary>
        ///     Get a database server by Id.
        /// </summary>
        /// <param name="databaseServerId">
        ///     The database server Id.
        /// </param>
        /// <returns>
        ///     The <see cref="DatabaseServer"/>, or <c>null</c> if no database server was found with the specified Id.
        /// </returns>
        public DatabaseServer GetDatabaseServerById(int databaseServerId) => DatabaseServers.FirstOrDefault(server => server.Id == databaseServerId);

        /// <summary>
        ///     Get a database server by tenant Id.
        /// </summary>
        /// <param name="tenantId">
        ///     The Id of the tenant that owns the database server.
        /// </param>
        /// <returns>
        ///     The <see cref="DatabaseServer"/>, or <c>null</c> if no database server was found with the specified tenant Id.
        /// </returns>
        public DatabaseServer GetDatabaseServerByTenantId(int tenantId) => DatabaseServers.FirstOrDefault(server => server.TenantId == tenantId);

        /// <summary>
        ///     Add a new <see cref="DatabaseServer"/>.
        /// </summary>
        /// <param name="tenantId">
        ///     The Id of the tenant that owns the database server.
        /// </param>
        /// <param name="name">
        ///     The server name.
        /// </param>
        /// <param name="adminPassword">
        ///     The server's administrative ("sa" user) password.
        /// </param>
        /// <param name="action">
        ///     The desired action for the server.
        /// </param>
        /// <param name="status">
        ///     The current status of the server.
        /// </param>
        /// <param name="phase">
        ///     The server's current provisioning phase.
        /// </param>
        /// <returns>
        ///     The new <see cref="DatabaseServer"/>.
        /// </returns>
        public DatabaseServer AddDatabaseServer(int tenantId, string name, string adminPassword, ProvisioningAction action = ProvisioningAction.Provision, ProvisioningStatus status = ProvisioningStatus.Pending, ServerProvisioningPhase phase = ServerProvisioningPhase.None)
        {
            if (String.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Argument cannot be null, empty, or entirely composed of whitespace: 'name'.", nameof(name));
            
            var server = new DatabaseServer
            {
                TenantId = tenantId,
                Name = name,
                AdminPassword = adminPassword,
                Action = action,
                Phase = ServerProvisioningPhase.None,
                Status = status
            };
            DatabaseServers.Add(server);

            return server;
        }

        /// <summary>
        ///     Determine if the specified server has any databases.
        /// </summary>
        /// <param name="serverId">
        ///     The server Id.
        /// </param>
        /// <returns>
        ///     <c>true</c> if the server has any databases; otherwise, <c>false</c>.
        /// </returns>
        public bool DoesServerHaveDatabases(int serverId) => DatabaseInstances.Any(database => database.DatabaseServerId == serverId);

        /// <summary>
        ///     Get a database instance by Id.
        /// </summary>
        /// <param name="databaseId">
        ///     The database instance Id.
        /// </param>
        /// <returns>
        ///     The <see cref="DatabaseInstance"/>, or <c>null</c> if no database instance was found with the specified Id.
        /// </returns>
        public DatabaseInstance GetDatabaseInstanceById(int databaseId) => DatabaseInstances.FirstOrDefault(database => database.Id == databaseId);

        /// <summary>
        ///     Get a database instance by name and server Id.
        /// </summary>
        /// <param name="name">
        ///     The name of the database to find.
        /// </param>
        /// <param name="serverId">
        ///     The Id of the server that hosts the database.
        /// </param>
        /// <returns>
        ///     The <see cref="DatabaseInstance"/>, or <c>null</c> if no database instance was found with the specified Id.
        /// </returns>
        public DatabaseInstance GetDatabaseInstanceByName(string name, int serverId) => DatabaseInstances.FirstOrDefault(database => database.Name == name && database.DatabaseServerId == serverId);

        /// <summary>
        ///     Get all <see cref="DatabaseInstance"/>s in the specified database server.
        /// </summary>
        /// <param name="serverId">
        ///     The database server Id.
        /// </param>
        /// <returns>
        ///     A sequence of <see cref="DatabaseInstance"/>s, sorted by name.
        /// </returns>
        public IEnumerable<DatabaseInstance> GetDatabaseInstancesByServer(int serverId)
        {
            return
                DatabaseInstances.Where(
                    database => database.DatabaseServerId == serverId
                )
                .OrderBy(database => database.Name)
                .AsEnumerable();
        }

        /// <summary>
        ///     Add a new <see cref="DatabaseInstance"/>.
        /// </summary>
        /// <param name="name">
        ///     The database name.
        /// </param>
        /// <param name="serverId">
        ///     The Id of the database server that will host the database.
        /// </param>
        /// <param name="databaseUser">
        ///     The database user name.
        /// </param>
        /// <param name="databasePassword">
        ///     The database user's password.
        /// </param>
        /// <param name="action">
        ///     The desired action for the database.
        /// </param>
        /// <param name="status">
        ///     The current status of the database.
        /// </param>
        /// <returns>
        ///     The new <see cref="DatabaseInstance"/>.
        /// </returns>
        public DatabaseInstance AddDatabaseInstance(string name, int serverId, string databaseUser, string databasePassword, ProvisioningAction action = ProvisioningAction.Provision, ProvisioningStatus status = ProvisioningStatus.Pending)
        {
            if (String.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Argument cannot be null, empty, or entirely composed of whitespace: 'name'.", nameof(name));

            if (String.IsNullOrWhiteSpace(databaseUser))
                throw new ArgumentException("Argument cannot be null, empty, or entirely composed of whitespace: 'databaseUser'.", nameof(databaseUser));
            
            if (String.IsNullOrWhiteSpace(databasePassword))
                throw new ArgumentException("Argument cannot be null, empty, or entirely composed of whitespace: 'databasePassword'.", nameof(databasePassword));
            
            var database = new DatabaseInstance
            {
                Name = name,
                DatabaseServerId = serverId,
                DatabaseUser = databaseUser,
                DatabasePassword = databasePassword,
                Action = action,
                Status = status
            };
            DatabaseInstances.Add(database);

            return database;
        }

        /// <summary>
        ///     Called when the entity model is being created.
        /// </summary>
        /// <param name="model">
        ///     The entity model.
        /// </param>
        protected override void OnModelCreating(ModelBuilder model)
        {
            model.Entity<Tenant>()
                .HasIndex(tenant => tenant.Name);

            model.Entity<DatabaseServer>()
                .HasIndex(server => server.Name);

            model.Entity<DatabaseInstance>()
                .HasIndex(database => database.Name);
        }
    }
}
