using Microsoft.Extensions.Configuration;

namespace DaaSDemo.Common.Options
{
    /// <summary>
    ///     Prometheus-related options for the DaaS API.
    /// </summary>
    public class PrometheusOptions
        : OptionsBase
    {
        /// <summary>
        ///     Enable Prometheus?
        /// </summary>
        public bool Enable { get; set; }

        /// <summary>
        ///     The base address of the Prometheus API end-point.
        /// </summary>
        public string ApiEndPoint { get; set; }

        /// <summary>
        ///     Load <see cref="PrometheusOptions"/> from configuration.
        /// </summary>
        /// <param name="configuration">
        ///     The <see cref="IConfiguration"/>.
        /// </param>
        /// <param name="key">
        ///     An optional sub-key name to load from.
        /// </param>
        /// <returns>
        ///     The <see cref="PrometheusOptions"/>.
        /// </returns>
        public static PrometheusOptions From(IConfiguration configuration, string key = "Prometheus") => Load<PrometheusOptions>(configuration, key);
    }
}
