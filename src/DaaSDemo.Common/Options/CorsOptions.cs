using Microsoft.Extensions.Configuration;
using System;

namespace DaaSDemo.Common.Options
{
    /// <summary>
    ///     CORS-related options for the DaaS API.
    /// </summary>
    public class CorsOptions
        : OptionsBase
    {
        /// <summary>
        ///     The base URIs used by the web UI.
        /// </summary>
        public string UI { get; set; }

        /// <summary>
        ///     Load <see cref="CorsOptions"/> from configuration.
        /// </summary>
        /// <param name="configuration">
        ///     The <see cref="IConfiguration"/>.
        /// </param>
        /// <param name="key">
        ///     An optional sub-key name to load from.
        /// </param>
        /// <returns>
        ///     The <see cref="CorsOptions"/>.
        /// </returns>
        public static CorsOptions From(IConfiguration configuration, string key = "CORS") => Load<CorsOptions>(configuration, key);
    }
}
