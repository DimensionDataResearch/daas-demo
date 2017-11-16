using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     IngressTLS describes the transport layer security associated with an Ingress.
    /// </summary>
    public class IngressTLSV1Beta1
    {
        /// <summary>
        ///     Hosts are a list of hosts included in the TLS certificate. The values in this list must match the name/s used in the tlsSecret. Defaults to the wildcard host setting for the loadbalancer controller fulfilling this Ingress, if left unspecified.
        /// </summary>
        [JsonProperty("hosts")]
        public List<string> Hosts { get; set; }
    }
}
