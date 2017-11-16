using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     Endpoints is a collection of endpoints that implement the actual service. Example:
    ///       Name: "mysvc",
    ///       Subsets: [
    ///         {
    ///           Addresses: [{"ip": "10.10.1.1"}, {"ip": "10.10.2.2"}],
    ///           Ports: [{"name": "a", "port": 8675}, {"name": "b", "port": 309}]
    ///         },
    ///         {
    ///           Addresses: [{"ip": "10.10.3.3"}],
    ///           Ports: [{"name": "a", "port": 93}, {"name": "b", "port": 76}]
    ///         },
    ///      ]
    /// </summary>
    public class EndpointsV1
    {
        /// <summary>
        ///     APIVersion defines the versioned schema of this representation of an object. Servers should convert recognized schemas to the latest internal value, and may reject unrecognized values. More info: https://git.k8s.io/community/contributors/devel/api-conventions.md#resources
        /// </summary>
        [JsonProperty("apiVersion")]
        public string ApiVersion { get; set; }

        /// <summary>
        ///     Kind is a string value representing the REST resource this object represents. Servers may infer this from the endpoint the client submits requests to. Cannot be updated. In CamelCase. More info: https://git.k8s.io/community/contributors/devel/api-conventions.md#types-kinds
        /// </summary>
        [JsonProperty("kind")]
        public string Kind { get; set; }

        /// <summary>
        ///     Standard object's metadata. More info: https://git.k8s.io/community/contributors/devel/api-conventions.md#metadata
        /// </summary>
        [JsonProperty("metadata")]
        public ObjectMetaV1 Metadata { get; set; }
    }
}
