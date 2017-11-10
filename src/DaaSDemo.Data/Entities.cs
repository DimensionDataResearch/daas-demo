using System;
using Microsoft.EntityFrameworkCore;

namespace DaaSDemo.Data
{
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
