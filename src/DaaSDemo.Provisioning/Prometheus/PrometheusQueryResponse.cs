using Newtonsoft.Json;

namespace DaaSDemo.Provisioning.Prometheus
{
    /// <summary>
    ///     Represents a query response from the Prometheus API.
    /// </summary>
    public class PrometheusQueryResponse
    {
        /// <summary>
        ///     The status of the query (sucess or failure).
        /// </summary>
        [JsonProperty("status")]
        public PrometheusResponseStatus Status { get; set; }

        /// <summary>
        ///     The query response data.
        /// </summary>
        [JsonProperty("data")]
        public PrometheusQueryResponseData Data { get; set; }
    }
}
