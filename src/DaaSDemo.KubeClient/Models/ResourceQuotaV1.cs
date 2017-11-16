using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     ResourceQuota sets aggregate quota restrictions enforced per namespace
    /// </summary>
    public class ResourceQuotaV1 : KubeResource
    {
        /// <summary>
        ///     Standard object's metadata. More info: https://git.k8s.io/community/contributors/devel/api-conventions.md#metadata
        /// </summary>
        [JsonProperty("metadata")]
        public ObjectMetaV1 Metadata { get; set; }

        /// <summary>
        ///     Spec defines the desired quota. https://git.k8s.io/community/contributors/devel/api-conventions.md#spec-and-status
        /// </summary>
        [JsonProperty("spec")]
        public ResourceQuotaSpecV1 Spec { get; set; }

        /// <summary>
        ///     Status defines the actual enforced quota and its current usage. https://git.k8s.io/community/contributors/devel/api-conventions.md#spec-and-status
        /// </summary>
        [JsonProperty("status")]
        public ResourceQuotaStatusV1 Status { get; set; }
    }
}
