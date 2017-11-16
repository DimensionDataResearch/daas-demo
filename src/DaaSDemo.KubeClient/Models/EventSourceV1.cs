using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     EventSource contains information for an event.
    /// </summary>
    public class EventSourceV1
    {
        /// <summary>
        ///     Component from which the event is generated.
        /// </summary>
        [JsonProperty("component")]
        public string Component { get; set; }

        /// <summary>
        ///     Node name on which the event is generated.
        /// </summary>
        [JsonProperty("host")]
        public string Host { get; set; }
    }
}
