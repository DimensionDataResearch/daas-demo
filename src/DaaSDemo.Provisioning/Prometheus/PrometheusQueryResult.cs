using Newtonsoft.Json;
using System.Collections.Generic;

namespace DaaSDemo.Provisioning.Prometheus
{
    /// <summary>
    ///     Represents a single result from a Prometheus query.
    /// </summary>
    public class PrometheusQueryResult
    {
        /// <summary>
        ///     Labels associated with the result.
        /// </summary>
        [JsonProperty("metric", ObjectCreationHandling = ObjectCreationHandling.Reuse)]
        public Dictionary<string, string> Labels { get; } = new Dictionary<string, string>();

        /// <summary>
        ///     The result value.
        /// </summary>
        [JsonProperty("value")]
        public PrometheusValue Value { get; set; }
    }
}
