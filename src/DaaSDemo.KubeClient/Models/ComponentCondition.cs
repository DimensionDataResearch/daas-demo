using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     Information about the condition of a component.
    /// </summary>
    public class ComponentConditionV1
    {
        /// <summary>
        ///     Condition error code for a component. For example, a health check error code.
        /// </summary>
        [JsonProperty("error")]
        public string Error { get; set; }

        /// <summary>
        ///     Message about the condition for a component. For example, information about a health check.
        /// </summary>
        [JsonProperty("message")]
        public string Message { get; set; }

        /// <summary>
        ///     Status of the condition for a component. Valid values for "Healthy": "True", "False", or "Unknown".
        /// </summary>
        [JsonProperty("status")]
        public string Status { get; set; }
    }
}
