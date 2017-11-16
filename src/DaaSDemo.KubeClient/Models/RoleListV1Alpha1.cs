using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     RoleList is a collection of Roles
    /// </summary>
    public class RoleListV1Alpha1 : KubeResource
    {
        /// <summary>
        ///     Standard object's metadata.
        /// </summary>
        [JsonProperty("metadata")]
        public ListMetaV1 Metadata { get; set; }

        /// <summary>
        ///     Items is a list of Roles
        /// </summary>
        [JsonProperty("items", NullValueHandling = NullValueHandling.Ignore)]
        public List<RoleV1Alpha1> Items { get; set; } = new List<RoleV1Alpha1>();
    }
}
