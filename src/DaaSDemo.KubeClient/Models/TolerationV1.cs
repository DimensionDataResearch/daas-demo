using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     The pod this Toleration is attached to tolerates any taint that matches the triple <key,value,effect> using the matching operator <operator>.
    /// </summary>
    public class TolerationV1
    {
        /// <summary>
        ///     Value is the taint value the toleration matches to. If the operator is Exists, the value should be empty, otherwise just a regular string.
        /// </summary>
        [JsonProperty("value")]
        public string Value { get; set; }

        /// <summary>
        ///     Operator represents a key's relationship to the value. Valid operators are Exists and Equal. Defaults to Equal. Exists is equivalent to wildcard for value, so that a pod can tolerate all taints of a particular category.
        /// </summary>
        [JsonProperty("operator")]
        public string Operator { get; set; }

        /// <summary>
        ///     TolerationSeconds represents the period of time the toleration (which must be of effect NoExecute, otherwise this field is ignored) tolerates the taint. By default, it is not set, which means tolerate the taint forever (do not evict). Zero and negative values will be treated as 0 (evict immediately) by the system.
        /// </summary>
        [JsonProperty("tolerationSeconds")]
        public int TolerationSeconds { get; set; }

        /// <summary>
        ///     Effect indicates the taint effect to match. Empty means match all taint effects. When specified, allowed values are NoSchedule, PreferNoSchedule and NoExecute.
        /// </summary>
        [JsonProperty("effect")]
        public string Effect { get; set; }

        /// <summary>
        ///     Key is the taint key that the toleration applies to. Empty means match all taint keys. If the key is empty, operator must be Exists; this combination means to match all values and all keys.
        /// </summary>
        [JsonProperty("key")]
        public string Key { get; set; }
    }
}
