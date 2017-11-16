using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     ClusterRoleList is a collection of ClusterRoles
    /// </summary>
    public class ClusterRoleListV1Alpha1 : KubeResource
    {
        /// <summary>
        ///     Standard object's metadata.
        /// </summary>
        [JsonProperty("metadata")]
        public ListMetaV1 Metadata { get; set; }

        /// <summary>
        ///     Items is a list of ClusterRoles
        /// </summary>
        [JsonProperty("items", NullValueHandling = NullValueHandling.Ignore)]
        public List<ClusterRoleV1Alpha1> Items { get; set; } = new List<ClusterRoleV1Alpha1>();
    }
}
