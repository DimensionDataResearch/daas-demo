using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     DaemonSetList is a collection of daemon sets.
    /// </summary>
    public class DaemonSetListV1Beta1 : KubeResourceListV1
    {
        /// <summary>
        ///     A list of daemon sets.
        /// </summary>
        [JsonProperty("items", NullValueHandling = NullValueHandling.Ignore)]
        public List<DaemonSetV1Beta1> Items { get; set; } = new List<DaemonSetV1Beta1>();
    }
}
