using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     SupplementalGroupsStrategyOptions defines the strategy type and options used to create the strategy.
    /// </summary>
    public class SupplementalGroupsStrategyOptionsV1Beta1
    {
        /// <summary>
        ///     Ranges are the allowed ranges of supplemental groups.  If you would like to force a single supplemental group then supply a single range with the same start and end.
        /// </summary>
        [JsonProperty("ranges")]
        public List<IDRangeV1Beta1> Ranges { get; set; }
    }
}
