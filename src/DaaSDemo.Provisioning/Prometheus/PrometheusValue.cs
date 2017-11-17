using Newtonsoft.Json;
using System;

namespace DaaSDemo.Provisioning.Prometheus
{
    using Converters;

    /// <summary>
    ///     Represents a result value from a Prometheus query.
    /// </summary>
    [JsonConverter(typeof(PrometheusValueConverter))]
    public class PrometheusValue
    {
        /// <summary>
        ///     The result timestamp.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        ///     The value.
        /// </summary>
        public string Value { get; set; }
    }
}
