using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     The weights of all of the matched WeightedPodAffinityTerm fields are added per-node to find the most preferred node(s)
    /// </summary>
    public class WeightedPodAffinityTermV1
    {
        /// <summary>
        ///     Required. A pod affinity term, associated with the corresponding weight.
        /// </summary>
        [JsonProperty("podAffinityTerm")]
        public PodAffinityTermV1 PodAffinityTerm { get; set; }
    }
}
