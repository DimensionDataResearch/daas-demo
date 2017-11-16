using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     represents the current status of a scale subresource.
    /// </summary>
    public class ScaleStatusV1Beta1
    {
        /// <summary>
        ///     actual number of observed instances of the scaled object.
        /// </summary>
        [JsonProperty("replicas")]
        public int Replicas { get; set; }

        /// <summary>
        ///     label query over pods that should match the replicas count. More info: http://kubernetes.io/docs/user-guide/labels#label-selectors
        /// </summary>
        [JsonProperty("selector")]
        public Dictionary<string, string> Selector { get; set; }
    }
}
