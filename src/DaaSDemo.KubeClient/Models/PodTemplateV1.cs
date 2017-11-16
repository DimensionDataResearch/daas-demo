using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     PodTemplate describes a template for creating copies of a predefined pod.
    /// </summary>
    public class PodTemplateV1 : KubeResource
    {
        /// <summary>
        ///     Standard object's metadata. More info: https://git.k8s.io/community/contributors/devel/api-conventions.md#metadata
        /// </summary>
        [JsonProperty("metadata")]
        public ObjectMetaV1 Metadata { get; set; }

        /// <summary>
        ///     Template defines the pods that will be created from this pod template. https://git.k8s.io/community/contributors/devel/api-conventions.md#spec-and-status
        /// </summary>
        [JsonProperty("template")]
        public PodTemplateSpecV1 Template { get; set; }
    }
}
