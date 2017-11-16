using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     EndpointPort is a tuple that describes a single port.
    /// </summary>
    public class EndpointPortV1
    {
        /// <summary>
        ///     The name of this port (corresponds to ServicePort.Name). Must be a DNS_LABEL. Optional only if one port is defined.
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        ///     The port number of the endpoint.
        /// </summary>
        [JsonProperty("port")]
        public int Port { get; set; }
    }
}
