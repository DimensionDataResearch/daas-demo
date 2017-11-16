using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     APIResourceList is a list of APIResource, it is used to expose the name of the resources supported in a specific group and version, and if the resource is namespaced.
    /// </summary>
    public class APIResourceListV1 : KubeResource
    {
        /// <summary>
        ///     groupVersion is the group and version this APIResourceList is for.
        /// </summary>
        [JsonProperty("groupVersion")]
        public string GroupVersion { get; set; }

        /// <summary>
        ///     resources contains the name of the resources and if they are namespaced.
        /// </summary>
        [JsonProperty("resources", NullValueHandling = NullValueHandling.Ignore)]
        public List<APIResourceV1> Resources { get; set; } = new List<APIResourceV1>();
    }
}
