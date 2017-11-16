using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     ResourceRequirements describes the compute resource requirements.
    /// </summary>
    public class ResourceRequirementsV1
    {
        /// <summary>
        ///     Limits describes the maximum amount of compute resources allowed. More info: https://kubernetes.io/docs/concepts/configuration/manage-compute-resources-container/
        /// </summary>
        [JsonProperty("limits")]
        public Dictionary<string, string> Limits { get; set; }
    }
}
