using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     APIGroupList is a list of APIGroup, to allow clients to discover the API at /apis.
    /// </summary>
    public class APIGroupListV1
    {
        /// <summary>
        ///     APIVersion defines the versioned schema of this representation of an object. Servers should convert recognized schemas to the latest internal value, and may reject unrecognized values. More info: https://git.k8s.io/community/contributors/devel/api-conventions.md#resources
        /// </summary>
        [JsonProperty("apiVersion")]
        public string ApiVersion { get; set; }

        /// <summary>
        ///     groups is a list of APIGroup.
        /// </summary>
        [JsonProperty("groups")]
        public List<APIGroupV1> Groups { get; set; }
    }
}
