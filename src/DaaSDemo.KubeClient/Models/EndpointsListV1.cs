using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     EndpointsList is a list of endpoints.
    /// </summary>
    public class EndpointsListV1 : KubeResource
    {
        /// <summary>
        ///     Standard list metadata. More info: https://git.k8s.io/community/contributors/devel/api-conventions.md#types-kinds
        /// </summary>
        [JsonProperty("metadata")]
        public ListMetaV1 Metadata { get; set; }

        /// <summary>
        ///     List of endpoints.
        /// </summary>
        [JsonProperty("items", NullValueHandling = NullValueHandling.Ignore)]
        public List<EndpointsV1> Items { get; set; } = new List<EndpointsV1>();
    }
}
