using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     SELinuxOptions are the labels to be applied to the container
    /// </summary>
    public class SELinuxOptionsV1
    {
        /// <summary>
        ///     Level is SELinux level label that applies to the container.
        /// </summary>
        [JsonProperty("level")]
        public string Level { get; set; }

        /// <summary>
        ///     Role is a SELinux role label that applies to the container.
        /// </summary>
        [JsonProperty("role")]
        public string Role { get; set; }

        /// <summary>
        ///     Type is a SELinux type label that applies to the container.
        /// </summary>
        [JsonProperty("type")]
        public string Type { get; set; }
    }
}
