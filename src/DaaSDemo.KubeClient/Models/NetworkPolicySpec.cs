using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     NetworkPolicySpec provides the specification of a NetworkPolicy
    /// </summary>
    public class NetworkPolicySpecV1
    {
        /// <summary>
        ///     List of ingress rules to be applied to the selected pods. Traffic is allowed to a pod if there are no NetworkPolicies selecting the pod (and cluster policy otherwise allows the traffic), OR if the traffic source is the pod's local node, OR if the traffic matches at least one ingress rule across all of the NetworkPolicy objects whose podSelector matches the pod. If this field is empty then this NetworkPolicy does not allow any traffic (and serves solely to ensure that the pods it selects are isolated by default)
        /// </summary>
        [JsonProperty("ingress")]
        public List<NetworkPolicyIngressRuleV1> Ingress { get; set; }
    }
}
