using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     DeploymentStrategy describes how to replace existing pods with new ones.
    /// </summary>
    public class DeploymentStrategyV1Beta1
    {
        /// <summary>
        ///     Rolling update config params. Present only if DeploymentStrategyType = RollingUpdate.
        /// </summary>
        [JsonProperty("rollingUpdate")]
        public RollingUpdateDeploymentV1Beta1 RollingUpdate { get; set; }
    }
}
