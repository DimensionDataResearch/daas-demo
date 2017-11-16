using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     Host Port Range defines a range of host ports that will be enabled by a policy for pods to use.  It requires both the start and end to be defined.
    /// </summary>
    public class HostPortRangeV1Beta1
    {
        /// <summary>
        ///     max is the end of the range, inclusive.
        /// </summary>
        [JsonProperty("max")]
        public int Max { get; set; }
    }
}
