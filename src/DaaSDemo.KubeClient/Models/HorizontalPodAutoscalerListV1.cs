using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     list of horizontal pod autoscaler objects.
    /// </summary>
    public class HorizontalPodAutoscalerListV1 : KubeResource
    {
        /// <summary>
        ///     Standard list metadata.
        /// </summary>
        [JsonProperty("metadata")]
        public ListMetaV1 Metadata { get; set; }

        /// <summary>
        ///     list of horizontal pod autoscaler objects.
        /// </summary>
        [JsonProperty("items", NullValueHandling = NullValueHandling.Ignore)]
        public List<HorizontalPodAutoscalerV1> Items { get; set; } = new List<HorizontalPodAutoscalerV1>();
    }
}
