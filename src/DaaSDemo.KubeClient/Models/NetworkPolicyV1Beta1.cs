using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     NetworkPolicy describes what network traffic is allowed for a set of Pods
    /// </summary>
    public class NetworkPolicyV1Beta1 : KubeResource
    {
        /// <summary>
        ///     Standard object's metadata. More info: https://git.k8s.io/community/contributors/devel/api-conventions.md#metadata
        /// </summary>
        [JsonProperty("metadata")]
        public ObjectMetaV1 Metadata { get; set; }

        /// <summary>
        ///     Specification of the desired behavior for this NetworkPolicy.
        /// </summary>
        [JsonProperty("spec")]
        public NetworkPolicySpecV1Beta1 Spec { get; set; }
    }
}
