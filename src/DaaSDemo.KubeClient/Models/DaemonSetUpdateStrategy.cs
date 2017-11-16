using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     No description provided.
    /// </summary>
    public class DaemonSetUpdateStrategyV1Beta1
    {
        /// <summary>
        ///     Rolling update config params. Present only if type = "RollingUpdate".
        /// </summary>
        [JsonProperty("rollingUpdate")]
        public RollingUpdateDaemonSetV1Beta1 RollingUpdate { get; set; }
    }
}
