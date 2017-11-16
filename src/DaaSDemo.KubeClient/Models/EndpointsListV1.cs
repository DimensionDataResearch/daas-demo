using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     EndpointsList is a list of endpoints.
    /// </summary>
    public class EndpointsListV1 : KubeResourceListV1
    {
        /// <summary>
        ///     List of endpoints.
        /// </summary>
        [JsonProperty("items", NullValueHandling = NullValueHandling.Ignore)]
        public List<EndpointsV1> Items { get; set; } = new List<EndpointsV1>();
    }
}
