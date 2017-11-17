using Newtonsoft.Json;
using System.Collections.Generic;

namespace DaaSDemo.Provisioning.Prometheus
{
    /// <summary>
    ///     Represents the data returned from a Prometheus query.
    /// </summary>
    public class PrometheusQueryResponseData
    {
        /// <summary>
        ///     The kind of result.
        /// </summary>
        [JsonProperty("resultType")]
        public PrometheusResultKind ResultKind { get; set; }

        /// <summary>
        ///     The query results.
        /// </summary>
        [JsonProperty("result", ObjectCreationHandling = ObjectCreationHandling.Reuse)]
        public List<PrometheusQueryResult> Results { get; } = new List<PrometheusQueryResult>();
    }
}
