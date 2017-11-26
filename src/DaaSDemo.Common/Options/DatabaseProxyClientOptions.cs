using Microsoft.Extensions.Configuration;

namespace DaaSDemo.Common.Options
{
    /// <summary>
    ///     DatabaseProxyClient-related options for the DaaS API.
    /// </summary>
    public class DatabaseProxyClientOptions
        : OptionsBase
    {
        /// <summary>
        ///     The base address of the Database Proxy API end-point.
        /// </summary>
        public string ApiEndPoint { get; set; }

        /// <summary>
        ///     Load <see cref="DatabaseProxyClientOptions"/> from configuration.
        /// </summary>
        /// <param name="configuration">
        ///     The <see cref="IConfiguration"/>.
        /// </param>
        /// <param name="key">
        ///     An optional sub-key name to load from.
        /// </param>
        /// <returns>
        ///     The <see cref="DatabaseProxyClientOptions"/>.
        /// </returns>
        public static DatabaseProxyClientOptions From(IConfiguration configuration, string key = "DatabaseProxy") => Load<DatabaseProxyClientOptions>(configuration, key);
    }
}
