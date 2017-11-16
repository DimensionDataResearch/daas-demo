using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     Pod Security Policy governs the ability to make requests that affect the Security Context that will be applied to a pod and container.
    /// </summary>
    public class PodSecurityPolicyV1Beta1 : KubeResourceV1
    {
        /// <summary>
        ///     spec defines the policy enforced.
        /// </summary>
        [JsonProperty("spec")]
        public PodSecurityPolicySpecV1Beta1 Spec { get; set; }
    }
}
