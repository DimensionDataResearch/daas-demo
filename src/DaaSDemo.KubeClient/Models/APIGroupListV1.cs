using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     APIGroupList is a list of APIGroup, to allow clients to discover the API at /apis.
    /// </summary>
    public class APIGroupListV1 : KubeResource
    {
        /// <summary>
        ///     groups is a list of APIGroup.
        /// </summary>
        [JsonProperty("groups", NullValueHandling = NullValueHandling.Ignore)]
        public List<APIGroupV1> Groups { get; set; } = new List<APIGroupV1>();
    }
}
