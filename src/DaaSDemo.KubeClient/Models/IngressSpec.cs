using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     IngressSpec describes the Ingress the user wishes to exist.
    /// </summary>
    public class IngressSpecV1Beta1
    {
        /// <summary>
        ///     A default backend capable of servicing requests that don't match any rule. At least one of 'backend' or 'rules' must be specified. This field is optional to allow the loadbalancer controller or defaulting logic to specify a global default.
        /// </summary>
        [JsonProperty("backend")]
        public IngressBackendV1Beta1 Backend { get; set; }

        /// <summary>
        ///     A list of host rules used to configure the Ingress. If unspecified, or no rule matches, all traffic is sent to the default backend.
        /// </summary>
        [JsonProperty("rules")]
        public List<IngressRuleV1Beta1> Rules { get; set; }
    }
}
