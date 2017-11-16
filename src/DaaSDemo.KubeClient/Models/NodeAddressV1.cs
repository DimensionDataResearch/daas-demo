using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     NodeAddress contains information for the node's address.
    /// </summary>
    public class NodeAddressV1
    {
        /// <summary>
        ///     Node address type, one of Hostname, ExternalIP or InternalIP.
        /// </summary>
        [JsonProperty("type")]
        public string Type { get; set; }

        /// <summary>
        ///     The node address.
        /// </summary>
        [JsonProperty("address")]
        public string Address { get; set; }
    }
}
