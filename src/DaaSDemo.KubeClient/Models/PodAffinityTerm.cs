using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     Defines a set of pods (namely those matching the labelSelector relative to the given namespace(s)) that this pod should be co-located (affinity) or not co-located (anti-affinity) with, where co-located is defined as running on a node whose value of the label with key <topologyKey> tches that of any node on which a pod of the set of pods is running
    /// </summary>
    public class PodAffinityTermV1
    {
        /// <summary>
        ///     A label query over a set of resources, in this case pods.
        /// </summary>
        [JsonProperty("labelSelector")]
        public LabelSelectorV1 LabelSelector { get; set; }

        /// <summary>
        ///     namespaces specifies which namespaces the labelSelector applies to (matches against); null or empty list means "this pod's namespace"
        /// </summary>
        [JsonProperty("namespaces")]
        public List<string> Namespaces { get; set; }
    }
}
