using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     PodList is a list of Pods.
    /// </summary>
    public class PodListV1 : KubeResource
    {
        /// <summary>
        ///     Standard list metadata. More info: https://git.k8s.io/community/contributors/devel/api-conventions.md#types-kinds
        /// </summary>
        [JsonProperty("metadata")]
        public ListMetaV1 Metadata { get; set; }

        /// <summary>
        ///     List of pods. More info: https://git.k8s.io/community/contributors/devel/api-conventions.md
        /// </summary>
        [JsonProperty("items", NullValueHandling = NullValueHandling.Ignore)]
        public List<PodV1> Items { get; set; } = new List<PodV1>();
    }
}
