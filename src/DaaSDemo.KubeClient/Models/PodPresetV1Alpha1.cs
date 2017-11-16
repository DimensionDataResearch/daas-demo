using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     PodPreset is a policy resource that defines additional runtime requirements for a Pod.
    /// </summary>
    public class PodPresetV1Alpha1 : KubeResourceV1
    {
        /// <summary>
        ///     Description not provided.
        /// </summary>
        [JsonProperty("spec")]
        public PodPresetSpecV1Alpha1 Spec { get; set; }
    }
}
