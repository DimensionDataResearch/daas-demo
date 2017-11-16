using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     PodPresetSpec is a description of a pod preset.
    /// </summary>
    public class PodPresetSpecV1Alpha1
    {
        /// <summary>
        ///     Env defines the collection of EnvVar to inject into containers.
        /// </summary>
        [JsonProperty("env")]
        public List<EnvVarV1> Env { get; set; }

        /// <summary>
        ///     EnvFrom defines the collection of EnvFromSource to inject into containers.
        /// </summary>
        [JsonProperty("envFrom")]
        public List<EnvFromSourceV1> EnvFrom { get; set; }

        /// <summary>
        ///     Selector is a label query over a set of resources, in this case pods. Required.
        /// </summary>
        [JsonProperty("selector")]
        public LabelSelectorV1 Selector { get; set; }

        /// <summary>
        ///     VolumeMounts defines the collection of VolumeMount to inject into containers.
        /// </summary>
        [JsonProperty("volumeMounts")]
        public List<VolumeMountV1> VolumeMounts { get; set; }
    }
}
