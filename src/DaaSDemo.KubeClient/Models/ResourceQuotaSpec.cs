using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     ResourceQuotaSpec defines the desired hard limits to enforce for Quota.
    /// </summary>
    public class ResourceQuotaSpecV1
    {
        /// <summary>
        ///     Hard is the set of desired hard limits for each named resource. More info: https://git.k8s.io/community/contributors/design-proposals/admission_control_resource_quota.md
        /// </summary>
        [JsonProperty("hard")]
        public Dictionary<string, string> Hard { get; set; }
    }
}
