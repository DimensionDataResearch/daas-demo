using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     TCPSocketAction describes an action based on opening a socket
    /// </summary>
    public class TCPSocketActionV1
    {
        /// <summary>
        ///     Optional: Host name to connect to, defaults to the pod IP.
        /// </summary>
        [JsonProperty("host")]
        public string Host { get; set; }
    }
}
