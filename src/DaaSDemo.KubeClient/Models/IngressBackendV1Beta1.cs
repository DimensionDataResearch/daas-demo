using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     IngressBackend describes all endpoints for a given service and port.
    /// </summary>
    public class IngressBackendV1Beta1
    {
        /// <summary>
        ///     Specifies the name of the referenced service.
        /// </summary>
        [JsonProperty("serviceName")]
        public string ServiceName { get; set; }

        /// <summary>
        ///     Specifies the port of the referenced service.
        /// </summary>
        [JsonProperty("servicePort")]
        public string ServicePort { get; set; }
    }
}
