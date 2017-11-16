using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     Adds and removes POSIX capabilities from running containers.
    /// </summary>
    public class CapabilitiesV1
    {
        /// <summary>
        ///     Added capabilities
        /// </summary>
        [JsonProperty("add")]
        public List<string> Add { get; set; }
    }
}
