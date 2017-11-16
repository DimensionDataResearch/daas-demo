using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     Pod Security Policy List is a list of PodSecurityPolicy objects.
    /// </summary>
    public class PodSecurityPolicyListV1Beta1 : KubeResourceListV1
    {
        /// <summary>
        ///     Items is a list of schema objects.
        /// </summary>
        [JsonProperty("items", NullValueHandling = NullValueHandling.Ignore)]
        public List<PodSecurityPolicyV1Beta1> Items { get; set; } = new List<PodSecurityPolicyV1Beta1>();
    }
}
