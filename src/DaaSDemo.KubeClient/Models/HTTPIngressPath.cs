using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     HTTPIngressPath associates a path regex with a backend. Incoming urls matching the path are forwarded to the backend.
    /// </summary>
    public class HTTPIngressPathV1Beta1
    {
        /// <summary>
        ///     Backend defines the referenced service endpoint to which the traffic will be forwarded to.
        /// </summary>
        [JsonProperty("backend")]
        public IngressBackendV1Beta1 Backend { get; set; }
    }
}
