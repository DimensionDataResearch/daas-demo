using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     ServiceSpec describes the attributes that a user creates on a service.
    /// </summary>
    public class ServiceSpecV1
    {
        /// <summary>
        ///     clusterIP is the IP address of the service and is usually assigned randomly by the master. If an address is specified manually and is not in use by others, it will be allocated to the service; otherwise, creation of the service will fail. This field can not be changed through updates. Valid values are "None", empty string (""), or a valid IP address. "None" can be specified for headless services when proxying is not required. Only applies to types ClusterIP, NodePort, and LoadBalancer. Ignored if type is ExternalName. More info: https://kubernetes.io/docs/concepts/services-networking/service/#virtual-ips-and-service-proxies
        /// </summary>
        [JsonProperty("clusterIP")]
        public string ClusterIP { get; set; }

        /// <summary>
        ///     externalIPs is a list of IP addresses for which nodes in the cluster will also accept traffic for this service.  These IPs are not managed by Kubernetes.  The user is responsible for ensuring that traffic arrives at a node with this IP.  A common example is external load-balancers that are not part of the Kubernetes system.
        /// </summary>
        [JsonProperty("externalIPs")]
        public List<string> ExternalIPs { get; set; }

        /// <summary>
        ///     externalName is the external reference that kubedns or equivalent will return as a CNAME record for this service. No proxying will be involved. Must be a valid DNS name and requires Type to be ExternalName.
        /// </summary>
        [JsonProperty("externalName")]
        public string ExternalName { get; set; }

        /// <summary>
        ///     externalTrafficPolicy denotes if this Service desires to route external traffic to node-local or cluster-wide endpoints. "Local" preserves the client source IP and avoids a second hop for LoadBalancer and Nodeport type services, but risks potentially imbalanced traffic spreading. "Cluster" obscures the client source IP and may cause a second hop to another node, but should have good overall load-spreading.
        /// </summary>
        [JsonProperty("externalTrafficPolicy")]
        public string ExternalTrafficPolicy { get; set; }

        /// <summary>
        ///     healthCheckNodePort specifies the healthcheck nodePort for the service. If not specified, HealthCheckNodePort is created by the service api backend with the allocated nodePort. Will use user-specified nodePort value if specified by the client. Only effects when Type is set to LoadBalancer and ExternalTrafficPolicy is set to Local.
        /// </summary>
        [JsonProperty("healthCheckNodePort")]
        public int HealthCheckNodePort { get; set; }

        /// <summary>
        ///     Only applies to Service Type: LoadBalancer LoadBalancer will get created with the IP specified in this field. This feature depends on whether the underlying cloud-provider supports specifying the loadBalancerIP when a load balancer is created. This field will be ignored if the cloud-provider does not support the feature.
        /// </summary>
        [JsonProperty("loadBalancerIP")]
        public string LoadBalancerIP { get; set; }

        /// <summary>
        ///     If specified and supported by the platform, this will restrict traffic through the cloud-provider load-balancer will be restricted to the specified client IPs. This field will be ignored if the cloud-provider does not support the feature." More info: https://kubernetes.io/docs/tasks/access-application-cluster/configure-cloud-provider-firewall/
        /// </summary>
        [JsonProperty("loadBalancerSourceRanges")]
        public List<string> LoadBalancerSourceRanges { get; set; }

        /// <summary>
        ///     The list of ports that are exposed by this service. More info: https://kubernetes.io/docs/concepts/services-networking/service/#virtual-ips-and-service-proxies
        /// </summary>
        [JsonProperty("ports")]
        public List<ServicePortV1> Ports { get; set; }

        /// <summary>
        ///     Route service traffic to pods with label keys and values matching this selector. If empty or not present, the service is assumed to have an external process managing its endpoints, which Kubernetes will not modify. Only applies to types ClusterIP, NodePort, and LoadBalancer. Ignored if type is ExternalName. More info: https://kubernetes.io/docs/concepts/services-networking/service/
        /// </summary>
        [JsonProperty("selector")]
        public Dictionary<string, string> Selector { get; set; }

        /// <summary>
        ///     Supports "ClientIP" and "None". Used to maintain session affinity. Enable client IP based session affinity. Must be ClientIP or None. Defaults to None. More info: https://kubernetes.io/docs/concepts/services-networking/service/#virtual-ips-and-service-proxies
        /// </summary>
        [JsonProperty("sessionAffinity")]
        public string SessionAffinity { get; set; }
    }
}
