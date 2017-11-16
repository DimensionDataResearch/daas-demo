using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     The node this Taint is attached to has the effect "effect" on any pod that that does not tolerate the Taint.
    /// </summary>
    public class TaintV1
    {
        /// <summary>
        ///     Required. The effect of the taint on pods that do not tolerate the taint. Valid effects are NoSchedule, PreferNoSchedule and NoExecute.
        /// </summary>
        [JsonProperty("effect")]
        public string Effect { get; set; }

        /// <summary>
        ///     Required. The taint key to be applied to a node.
        /// </summary>
        [JsonProperty("key")]
        public string Key { get; set; }

        /// <summary>
        ///     TimeAdded represents the time at which the taint was added. It is only written for NoExecute taints.
        /// </summary>
        [JsonProperty("timeAdded")]
        public DateTime TimeAdded { get; set; }
    }
}
