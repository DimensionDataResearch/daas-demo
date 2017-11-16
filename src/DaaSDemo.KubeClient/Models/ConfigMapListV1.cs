using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     ConfigMapList is a resource containing a list of ConfigMap objects.
    /// </summary>
    public class ConfigMapListV1 : KubeResource
    {
        /// <summary>
        ///     More info: https://git.k8s.io/community/contributors/devel/api-conventions.md#metadata
        /// </summary>
        [JsonProperty("metadata")]
        public ListMetaV1 Metadata { get; set; }

        /// <summary>
        ///     Items is the list of ConfigMaps.
        /// </summary>
        [JsonProperty("items", NullValueHandling = NullValueHandling.Ignore)]
        public List<ConfigMapV1> Items { get; set; } = new List<ConfigMapV1>();
    }
}
