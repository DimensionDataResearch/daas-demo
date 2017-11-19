using Microsoft.Extensions.Configuration;

namespace DaaSDemo.Common.Options
{
    /// <summary>
    ///     SqlExecutorClient-related options for the DaaS API.
    /// </summary>
    public class SqlExecutorClientOptions
        : OptionsBase
    {
        /// <summary>
        ///     The base address of the SQL Executor API end-point.
        /// </summary>
        public string ApiEndPoint { get; set; }

        /// <summary>
        ///     Load <see cref="SqlExecutorClientOptions"/> from configuration.
        /// </summary>
        /// <param name="configuration">
        ///     The <see cref="IConfiguration"/>.
        /// </param>
        /// <param name="key">
        ///     An optional sub-key name to load from.
        /// </param>
        /// <returns>
        ///     The <see cref="SqlExecutorClientOptions"/>.
        /// </returns>
        public static SqlExecutorClientOptions From(IConfiguration configuration, string key = "SQL") => Load<SqlExecutorClientOptions>(configuration, key);
    }
}
