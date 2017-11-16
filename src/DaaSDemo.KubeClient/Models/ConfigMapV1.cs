using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     ConfigMap holds configuration data for pods to consume.
    /// </summary>
    public class ConfigMapV1 : KubeResource
    {
        /// <summary>
        ///     Data contains the configuration data. Each key must consist of alphanumeric characters, '-', '_' or '.'.
        /// </summary>
        [JsonProperty("data", NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, string> Data { get; set; } = new Dictionary<string, string>();

        /// <summary>
        ///     Standard object's metadata. More info: https://git.k8s.io/community/contributors/devel/api-conventions.md#metadata
        /// </summary>
        [JsonProperty("metadata")]
        public ObjectMetaV1 Metadata { get; set; }
    }
}
