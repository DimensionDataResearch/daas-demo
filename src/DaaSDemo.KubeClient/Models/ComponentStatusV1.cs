using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     ComponentStatus (and ComponentStatusList) holds the cluster validation info.
    /// </summary>
    public class ComponentStatusV1 : KubeResourceV1
    {
        /// <summary>
        ///     List of component conditions observed
        /// </summary>
        [JsonProperty("conditions", NullValueHandling = NullValueHandling.Ignore)]
        public List<ComponentConditionV1> Conditions { get; set; } = new List<ComponentConditionV1>();
    }
}
