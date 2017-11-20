using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DaaSDemo.SqlExecutor.Client
{
    using Common.Options;

    /// <summary>
    ///     Extension methods for registering <see cref="SqlApiClient"/> as a component.
    /// </summary>
    public static class ClientRegistrationExtensions
    {
        /// <summary>
        ///     Add an <see cref="SqlApiClient"/> to the service collection.
        /// </summary>
        /// <param name="services">
        ///     The service collection.
        /// </param>
        public static void AddSqlApiClient(this IServiceCollection services)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));
            
            services.AddScoped<SqlApiClient>(serviceProvider =>
            {
                SqlExecutorClientOptions clientOptions = serviceProvider.GetRequiredService<IOptions<SqlExecutorClientOptions>>().Value;
                
                if (String.IsNullOrWhiteSpace(clientOptions.ApiEndPoint))
                    throw new InvalidOperationException("Application configuration is missing SQL Executor API end-point.");

                return SqlApiClient.Create(
                    endPointUri: new Uri(clientOptions.ApiEndPoint)
                );
            });
        }
    }
}
