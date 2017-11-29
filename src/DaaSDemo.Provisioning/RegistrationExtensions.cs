using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;

namespace DaaSDemo.Provisioning
{
    using Common.Options;
    using Provisioners;

    /// <summary>
    ///     Extension methods for registering provisioning components.
    /// </summary>
    public static class RegistrationExtensions
    {
        /// <summary>
        ///     Register provisioning components and services.
        /// </summary>
        /// <param name="services">
        ///     The service collection to configure.
        /// </param>
        public static void AddProvisioning(this IServiceCollection services)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            services.AddTransient<KubeNames>();
            services.AddTransient<KubeSpecs>();
            services.AddTransient<KubeResources>();

            services.AddTransient<ServerCredentialsProvisioner>();
            services.AddTransient<DatabaseServerProvisioner>();
            services.AddTransient<DatabaseProvisioner, RavenDatabaseProvisioner>();
            services.AddTransient<DatabaseProvisioner, SqlServerDatabaseProvisioner>();

            services.AddTransient<Actors.DataAccess>();
            services.AddTransient<Actors.TenantServerManager>();
            services.AddTransient<Actors.TenantDatabaseManager>();

            services.AddSingleton<ProvisioningEngine>();
        }
    }
}
