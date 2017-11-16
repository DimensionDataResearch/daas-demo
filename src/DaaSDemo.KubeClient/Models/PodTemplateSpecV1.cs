using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     PodTemplateSpec describes the data a pod should have when created from a template
    /// </summary>
    public class PodTemplateSpecV1
    {
        /// <summary>
        ///     Specification of the desired behavior of the pod. More info: https://git.k8s.io/community/contributors/devel/api-conventions.md#spec-and-status
        /// </summary>
        [JsonProperty("spec")]
        public PodSpecV1 Spec { get; set; }
    }
}
