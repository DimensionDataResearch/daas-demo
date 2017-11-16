using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     ID Range provides a min/max of an allowed range of IDs.
    /// </summary>
    public class IDRangeV1Beta1
    {
        /// <summary>
        ///     Min is the start of the range, inclusive.
        /// </summary>
        [JsonProperty("min")]
        public int Min { get; set; }

        /// <summary>
        ///     Max is the end of the range, inclusive.
        /// </summary>
        [JsonProperty("max")]
        public int Max { get; set; }
    }
}
