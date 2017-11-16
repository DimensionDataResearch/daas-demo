using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     ServicePort contains information on service's port.
    /// </summary>
    public class ServicePortV1
    {
        /// <summary>
        ///     The name of this port within the service. This must be a DNS_LABEL. All ports within a ServiceSpec must have unique names. This maps to the 'Name' field in EndpointPort objects. Optional if only one ServicePort is defined on this service.
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        ///     The port on each node on which this service is exposed when type=NodePort or LoadBalancer. Usually assigned by the system. If specified, it will be allocated to the service if unused or else creation of the service will fail. Default is to auto-allocate a port if the ServiceType of this Service requires one. More info: https://kubernetes.io/docs/concepts/services-networking/service/#type-nodeport
        /// </summary>
        [JsonProperty("nodePort")]
        public int NodePort { get; set; }

        /// <summary>
        ///     The port that will be exposed by this service.
        /// </summary>
        [JsonProperty("port")]
        public int Port { get; set; }

        /// <summary>
        ///     The IP protocol for this port. Supports "TCP" and "UDP". Default is TCP.
        /// </summary>
        [JsonProperty("protocol")]
        public string Protocol { get; set; }
    }
}
