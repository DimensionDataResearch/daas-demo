using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     ScaleStatus represents the current status of a scale subresource.
    /// </summary>
    public class ScaleStatusV1
    {
        /// <summary>
        ///     label query over pods that should match the replicas count. This is same as the label selector but in the string format to avoid introspection by clients. The string will be in the same format as the query-param syntax. More info about label selectors: http://kubernetes.io/docs/user-guide/labels#label-selectors
        /// </summary>
        [JsonProperty("selector")]
        public string Selector { get; set; }

        /// <summary>
        ///     actual number of observed instances of the scaled object.
        /// </summary>
        [JsonProperty("replicas")]
        public int Replicas { get; set; }
    }
}
