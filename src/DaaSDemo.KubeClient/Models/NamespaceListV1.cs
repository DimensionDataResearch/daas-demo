using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     NamespaceList is a list of Namespaces.
    /// </summary>
    public class NamespaceListV1 : KubeResource
    {
        /// <summary>
        ///     Standard list metadata. More info: https://git.k8s.io/community/contributors/devel/api-conventions.md#types-kinds
        /// </summary>
        [JsonProperty("metadata")]
        public ListMetaV1 Metadata { get; set; }

        /// <summary>
        ///     Items is the list of Namespace objects in the list. More info: https://kubernetes.io/docs/concepts/overview/working-with-objects/namespaces/
        /// </summary>
        [JsonProperty("items", NullValueHandling = NullValueHandling.Ignore)]
        public List<NamespaceV1> Items { get; set; } = new List<NamespaceV1>();
    }
}
