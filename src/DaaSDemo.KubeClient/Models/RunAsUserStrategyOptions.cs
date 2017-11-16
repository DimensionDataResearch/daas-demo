using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     Run A sUser Strategy Options defines the strategy type and any options used to create the strategy.
    /// </summary>
    public class RunAsUserStrategyOptionsV1Beta1
    {
        /// <summary>
        ///     Ranges are the allowed ranges of uids that may be used.
        /// </summary>
        [JsonProperty("ranges")]
        public List<IDRangeV1Beta1> Ranges { get; set; }
    }
}
