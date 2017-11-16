using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     ClusterRoleBindingList is a collection of ClusterRoleBindings
    /// </summary>
    public class ClusterRoleBindingListV1Beta1 : KubeResource
    {
        /// <summary>
        ///     Standard object's metadata.
        /// </summary>
        [JsonProperty("metadata")]
        public ListMetaV1 Metadata { get; set; }

        /// <summary>
        ///     Items is a list of ClusterRoleBindings
        /// </summary>
        [JsonProperty("items", NullValueHandling = NullValueHandling.Ignore)]
        public List<ClusterRoleBindingV1Beta1> Items { get; set; } = new List<ClusterRoleBindingV1Beta1>();
    }
}
