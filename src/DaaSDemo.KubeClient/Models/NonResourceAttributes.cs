using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     NonResourceAttributes includes the authorization attributes available for non-resource requests to the Authorizer interface
    /// </summary>
    public class NonResourceAttributesV1Beta1
    {
        /// <summary>
        ///     Path is the URL path of the request
        /// </summary>
        [JsonProperty("path")]
        public string Path { get; set; }
    }
}
