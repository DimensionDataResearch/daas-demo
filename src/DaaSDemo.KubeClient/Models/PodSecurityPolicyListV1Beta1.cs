using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     Pod Security Policy List is a list of PodSecurityPolicy objects.
    /// </summary>
    public class PodSecurityPolicyListV1Beta1 : KubeResource
    {
        /// <summary>
        ///     Standard list metadata. More info: https://git.k8s.io/community/contributors/devel/api-conventions.md#metadata
        /// </summary>
        [JsonProperty("metadata")]
        public ListMetaV1 Metadata { get; set; }

        /// <summary>
        ///     Items is a list of schema objects.
        /// </summary>
        [JsonProperty("items", NullValueHandling = NullValueHandling.Ignore)]
        public List<PodSecurityPolicyV1Beta1> Items { get; set; } = new List<PodSecurityPolicyV1Beta1>();
    }
}
