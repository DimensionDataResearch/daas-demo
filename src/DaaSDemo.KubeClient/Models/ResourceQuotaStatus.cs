using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     ResourceQuotaStatus defines the enforced hard limits and observed use.
    /// </summary>
    public class ResourceQuotaStatusV1
    {
        /// <summary>
        ///     Hard is the set of enforced hard limits for each named resource. More info: https://git.k8s.io/community/contributors/design-proposals/admission_control_resource_quota.md
        /// </summary>
        [JsonProperty("hard")]
        public Dictionary<string, string> Hard { get; set; }
    }
}
