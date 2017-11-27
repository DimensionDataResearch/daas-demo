using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using VaultSharp;
using VaultSharp.Backends.Authentication.Models.Token;

namespace DaaSDemo.Crypto
{
    using Common.Options;

    /// <summary>
    ///     Extension methods for registering cryptographic components in a service collection.
    /// </summary>
    public static class RegistrationExtensions
    {
        /// <summary>
        ///     Add the Vault API client to the service collection.
        /// </summary>
        /// <param name="services">
        ///     The service collection to configure.
        /// </param>
        public static void AddVaultClient(this IServiceCollection services)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            services.AddSingleton<IVaultClient>(serviceProvider =>
            {
                VaultOptions vaultOptions = serviceProvider.GetRequiredService<IOptions<VaultOptions>>().Value;

                if (String.IsNullOrWhiteSpace(vaultOptions.EndPoint))
                    throw new InvalidOperationException("Application configuration is missing Vault end-point.");

                if (String.IsNullOrWhiteSpace(vaultOptions.Token))
                    throw new InvalidOperationException("Application configuration is missing Vault access token.");

                return VaultClientFactory.CreateVaultClient(
                    new Uri(vaultOptions.EndPoint),
                    new TokenAuthenticationInfo(vaultOptions.Token)
                );
            });
        }
    }
}
