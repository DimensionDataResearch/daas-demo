using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DaaSDemo.DatabaseProxy.Client
{
    using Common.Options;

    /// <summary>
    ///     Extension methods for registering <see cref="DatabaseProxyApiClient"/> as a component.
    /// </summary>
    public static class ClientRegistrationExtensions
    {
        /// <summary>
        ///     Add an <see cref="DatabaseProxyApiClient"/> to the service collection.
        /// </summary>
        /// <param name="services">
        ///     The service collection.
        /// </param>
        public static void AddDatabaseProxyApiClient(this IServiceCollection services)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));
            
            services.AddScoped<DatabaseProxyApiClient>(serviceProvider =>
            {
                DatabaseProxyClientOptions clientOptions = serviceProvider.GetRequiredService<IOptions<DatabaseProxyClientOptions>>().Value;
                
                if (String.IsNullOrWhiteSpace(clientOptions.ApiEndPoint))
                    throw new InvalidOperationException("Application configuration is missing Database Proxy API end-point.");

                return DatabaseProxyApiClient.Create(
                    endPointUri: new Uri(clientOptions.ApiEndPoint)
                );
            });
        }
    }
}
