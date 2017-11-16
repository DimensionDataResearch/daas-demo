using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     ServiceStatus represents the current status of a service.
    /// </summary>
    public class ServiceStatusV1
    {
        /// <summary>
        ///     LoadBalancer contains the current status of the load-balancer, if one is present.
        /// </summary>
        [JsonProperty("loadBalancer")]
        public LoadBalancerStatusV1 LoadBalancer { get; set; }
    }
}
