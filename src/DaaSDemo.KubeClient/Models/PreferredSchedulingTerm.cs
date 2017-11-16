using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     An empty preferred scheduling term matches all objects with implicit weight 0 (i.e. it's a no-op). A null preferred scheduling term matches no objects (i.e. is also a no-op).
    /// </summary>
    public class PreferredSchedulingTermV1
    {
        /// <summary>
        ///     A node selector term, associated with the corresponding weight.
        /// </summary>
        [JsonProperty("preference")]
        public NodeSelectorTermV1 Preference { get; set; }
    }
}
