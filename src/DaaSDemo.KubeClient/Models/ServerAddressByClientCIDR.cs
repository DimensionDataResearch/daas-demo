using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     ServerAddressByClientCIDR helps the client to determine the server address that they should use, depending on the clientCIDR that they match.
    /// </summary>
    public class ServerAddressByClientCIDRV1
    {
        /// <summary>
        ///     The CIDR with which clients can match their IP to figure out the server address that they should use.
        /// </summary>
        [JsonProperty("clientCIDR")]
        public string ClientCIDR { get; set; }
    }
}
