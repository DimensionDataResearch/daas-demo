using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     Role is a namespaced, logical grouping of PolicyRules that can be referenced as a unit by a RoleBinding.
    /// </summary>
    public class RoleV1Beta1 : KubeResource
    {
        /// <summary>
        ///     Standard object's metadata.
        /// </summary>
        [JsonProperty("metadata")]
        public ObjectMetaV1 Metadata { get; set; }

        /// <summary>
        ///     Rules holds all the PolicyRules for this Role
        /// </summary>
        [JsonProperty("rules", NullValueHandling = NullValueHandling.Ignore)]
        public List<PolicyRuleV1Beta1> Rules { get; set; } = new List<PolicyRuleV1Beta1>();
    }
}
