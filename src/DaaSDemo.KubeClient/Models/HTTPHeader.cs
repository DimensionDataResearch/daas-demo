using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     HTTPHeader describes a custom header to be used in HTTP probes
    /// </summary>
    public class HTTPHeaderV1
    {
        /// <summary>
        ///     The header field name
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }
    }
}
