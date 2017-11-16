using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     LimitRangeItem defines a min/max usage limit for any resource that matches on kind.
    /// </summary>
    public class LimitRangeItemV1
    {
        /// <summary>
        ///     Default resource requirement limit value by resource name if resource limit is omitted.
        /// </summary>
        [JsonProperty("default")]
        public Dictionary<string, string> Default { get; set; }

        /// <summary>
        ///     DefaultRequest is the default resource requirement request value by resource name if resource request is omitted.
        /// </summary>
        [JsonProperty("defaultRequest")]
        public Dictionary<string, string> DefaultRequest { get; set; }

        /// <summary>
        ///     Max usage constraints on this kind by resource name.
        /// </summary>
        [JsonProperty("max")]
        public Dictionary<string, string> Max { get; set; }

        /// <summary>
        ///     MaxLimitRequestRatio if specified, the named resource must have a request and limit that are both non-zero where limit divided by request is less than or equal to the enumerated value; this represents the max burst for the named resource.
        /// </summary>
        [JsonProperty("maxLimitRequestRatio")]
        public Dictionary<string, string> MaxLimitRequestRatio { get; set; }

        /// <summary>
        ///     Min usage constraints on this kind by resource name.
        /// </summary>
        [JsonProperty("min")]
        public Dictionary<string, string> Min { get; set; }
    }
}
