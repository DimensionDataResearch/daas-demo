using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     StatefulSetList is a collection of StatefulSets.
    /// </summary>
    public class StatefulSetListV1Beta1 : KubeResource
    {
        /// <summary>
        ///     Description not provided.
        /// </summary>
        [JsonProperty("metadata")]
        public ListMetaV1 Metadata { get; set; }

        /// <summary>
        ///     Description not provided.
        /// </summary>
        [JsonProperty("items", NullValueHandling = NullValueHandling.Ignore)]
        public List<StatefulSetV1Beta1> Items { get; set; } = new List<StatefulSetV1Beta1>();
    }
}
