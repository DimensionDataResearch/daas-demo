using Microsoft.Extensions.Configuration;

namespace DaaSDemo.Common.Options
{
    /// <summary>
    ///     Database-related options for the DaaS API.
    /// </summary>
    public class DatabaseOptions
        : OptionsBase
    {
        /// <summary>
        ///     The database connection string.
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        ///     Load <see cref="DatabaseOptions"/> from configuration.
        /// </summary>
        /// <param name="configuration">
        ///     The <see cref="IConfiguration"/>.
        /// </param>
        /// <param name="key">
        ///     An optional sub-key name to load from.
        /// </param>
        /// <returns>
        ///     The <see cref="DatabaseOptions"/>.
        /// </returns>
        public static DatabaseOptions From(IConfiguration configuration, string key = "Database") => Load<DatabaseOptions>(configuration, key);
    }
}
