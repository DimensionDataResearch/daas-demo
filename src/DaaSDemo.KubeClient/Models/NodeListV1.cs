using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     NodeList is the whole list of all Nodes which have been registered with master.
    /// </summary>
    public class NodeListV1 : KubeResourceListV1
    {
        /// <summary>
        ///     List of nodes
        /// </summary>
        [JsonProperty("items", NullValueHandling = NullValueHandling.Ignore)]
        public List<NodeV1> Items { get; set; } = new List<NodeV1>();
    }
}
