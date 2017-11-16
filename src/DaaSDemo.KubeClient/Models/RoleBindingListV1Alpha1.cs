using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     RoleBindingList is a collection of RoleBindings
    /// </summary>
    public class RoleBindingListV1Alpha1 : KubeResource
    {
        /// <summary>
        ///     Standard object's metadata.
        /// </summary>
        [JsonProperty("metadata")]
        public ListMetaV1 Metadata { get; set; }

        /// <summary>
        ///     Items is a list of RoleBindings
        /// </summary>
        [JsonProperty("items", NullValueHandling = NullValueHandling.Ignore)]
        public List<RoleBindingV1Alpha1> Items { get; set; } = new List<RoleBindingV1Alpha1>();
    }
}
