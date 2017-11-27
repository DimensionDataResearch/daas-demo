using Microsoft.Extensions.Configuration;
using System;

namespace DaaSDemo.Common.Options
{
    /// <summary>
    ///     Vault-related options for the DaaS demo system.
    /// </summary>
    public class VaultOptions
        : OptionsBase
    {
        /// <summary>
        ///     The base address of the Vault API end-point.
        /// </summary>
        public string EndPoint { get; set; }

        /// <summary>
        ///     The access token to use for authentication to Vault.
        /// </summary>
        public string Token { get; set; }

        /// <summary>
        ///     The base mount-path in Vault for PKI facilities.
        /// </summary>
        public string PkiBasePath { get; set; }

        /// <summary>
        ///     The name of the default issuance policy for certificate credentials.
        /// </summary>
        public string CertificatePolicyName { get; set; }

        /// <summary>
        ///     Load <see cref="VaultOptions"/> from configuration.
        /// </summary>
        /// <param name="configuration">
        ///     The <see cref="IConfiguration"/>.
        /// </param>
        /// <param name="key">
        ///     An optional sub-key name to load from.
        /// </param>
        /// <returns>
        ///     The <see cref="VaultOptions"/>.
        /// </returns>
        public static VaultOptions From(IConfiguration configuration, string key = "Vault") => Load<VaultOptions>(configuration, key);
    }
}
