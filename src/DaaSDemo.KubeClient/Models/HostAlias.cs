using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     HostAlias holds the mapping between IP and hostnames that will be injected as an entry in the pod's hosts file.
    /// </summary>
    public class HostAliasV1
    {
        /// <summary>
        ///     Hostnames for the above IP address.
        /// </summary>
        [JsonProperty("hostnames")]
        public List<string> Hostnames { get; set; }
    }
}
