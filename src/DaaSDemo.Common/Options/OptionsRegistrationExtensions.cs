using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;

namespace DaaSDemo.Common.Options
{
    using Options;

    /// <summary>
    ///     Extension methods for registration of DaaS application options.
    /// </summary>
    public static class OptionsRegistrationExtensions
    {
        /// <summary>
        ///     Add and configure application-level options.
        /// </summary>
        /// <param name="services">
        ///     The application-level service collection.
        /// </param>
        /// <param name="configuration">
        ///     The application-level configuration.
        /// </param>
        public static void AddDaaSOptions(this IServiceCollection services, IConfiguration configuration)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));
            
            services.Configure<DatabaseOptions>(
                configuration.GetSection("Database")
            );
            services.Configure<SqlExecutorClientOptions>(
                configuration.GetSection("SQL")
            );
            services.Configure<KubernetesOptions>(
                configuration.GetSection("Kubernetes")
            );
            services.Configure<PrometheusOptions>(
                configuration.GetSection("Prometheus")
            );
            services.Configure<ProvisioningOptions>(
                configuration.GetSection("Provisioning")
            );
        }
    }
}
