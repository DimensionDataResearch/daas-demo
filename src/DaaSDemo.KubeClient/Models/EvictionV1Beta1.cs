using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     Eviction evicts a pod from its node subject to certain policies and safety constraints. This is a subresource of Pod.  A request to cause such an eviction is created by POSTing to .../pods/<pod name>/evictions.
    /// </summary>
    public class EvictionV1Beta1 : KubeResourceV1
    {
        /// <summary>
        ///     DeleteOptions may be provided
        /// </summary>
        [JsonProperty("deleteOptions")]
        public DeleteOptionsV1 DeleteOptions { get; set; }
    }
}
