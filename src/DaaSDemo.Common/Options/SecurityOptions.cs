using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;

namespace DaaSDemo.Common.Options
{
    /// <summary>
    ///     Security-related options.
    /// </summary>
    public class SecurityOptions
        : OptionsBase
    {
        /// <summary>
        ///     The base address of the identity server.
        /// </summary>
        public string IdentityServerBaseAddress { get; set; }

        /// <summary>
        ///     The base addresses of the DaaS portal
        /// </summary>
        public List<string> PortalBaseAddresses { get; set; } = new List<string>();

        /// <summary>
        ///     Load <see cref="SecurityOptions"/> from configuration.
        /// </summary>
        /// <param name="configuration">
        ///     The <see cref="IConfiguration"/>.
        /// </param>
        /// <param name="key">
        ///     An optional sub-key name to load from.
        /// </param>
        /// <returns>
        ///     The <see cref="SecurityOptions"/>.
        /// </returns>
        public static SecurityOptions From(IConfiguration configuration, string key = "Security") => Load<SecurityOptions>(configuration, key);
    }
}
