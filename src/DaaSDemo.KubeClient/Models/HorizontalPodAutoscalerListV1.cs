using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     list of horizontal pod autoscaler objects.
    /// </summary>
    public class HorizontalPodAutoscalerListV1 : KubeResourceListV1
    {
        /// <summary>
        ///     list of horizontal pod autoscaler objects.
        /// </summary>
        [JsonProperty("items", NullValueHandling = NullValueHandling.Ignore)]
        public List<HorizontalPodAutoscalerV1> Items { get; set; } = new List<HorizontalPodAutoscalerV1>();
    }
}
