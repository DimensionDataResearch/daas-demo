using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     Handler defines a specific action that should be taken
    /// </summary>
    public class HandlerV1
    {
        /// <summary>
        ///     One and only one of the following should be specified. Exec specifies the action to take.
        /// </summary>
        [JsonProperty("exec")]
        public ExecActionV1 Exec { get; set; }

        /// <summary>
        ///     HTTPGet specifies the http request to perform.
        /// </summary>
        [JsonProperty("httpGet")]
        public HTTPGetActionV1 HttpGet { get; set; }
    }
}
