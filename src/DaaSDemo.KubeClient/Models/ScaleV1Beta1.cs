using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     represents a scaling request for a resource.
    /// </summary>
    public class ScaleV1Beta1 : KubeResourceV1
    {
        /// <summary>
        ///     defines the behavior of the scale. More info: https://git.k8s.io/community/contributors/devel/api-conventions.md#spec-and-status.
        /// </summary>
        [JsonProperty("spec")]
        public ScaleSpecV1Beta1 Spec { get; set; }

        /// <summary>
        ///     current status of the scale. More info: https://git.k8s.io/community/contributors/devel/api-conventions.md#spec-and-status. Read-only.
        /// </summary>
        [JsonProperty("status")]
        public ScaleStatusV1Beta1 Status { get; set; }
    }
}
