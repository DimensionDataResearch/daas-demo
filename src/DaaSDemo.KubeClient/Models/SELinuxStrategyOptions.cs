using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     SELinux  Strategy Options defines the strategy type and any options used to create the strategy.
    /// </summary>
    public class SELinuxStrategyOptionsV1Beta1
    {
        /// <summary>
        ///     type is the strategy that will dictate the allowable labels that may be set.
        /// </summary>
        [JsonProperty("rule")]
        public string Rule { get; set; }
    }
}
