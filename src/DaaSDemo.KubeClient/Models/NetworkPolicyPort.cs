using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     NetworkPolicyPort describes a port to allow traffic on
    /// </summary>
    public class NetworkPolicyPortV1
    {
        /// <summary>
        ///     The port on the given protocol. This can either be a numerical or named port on a pod. If this field is not provided, this matches all port names and numbers.
        /// </summary>
        [JsonProperty("port")]
        public string Port { get; set; }
    }
}
