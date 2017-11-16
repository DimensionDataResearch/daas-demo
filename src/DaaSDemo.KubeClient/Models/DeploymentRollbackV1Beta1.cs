using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     DeploymentRollback stores the information required to rollback a deployment.
    /// </summary>
    public class DeploymentRollbackV1Beta1 : KubeResource
    {
        /// <summary>
        ///     Required: This must match the Name of a deployment.
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        ///     The config of this deployment rollback.
        /// </summary>
        [JsonProperty("rollbackTo")]
        public RollbackConfigV1Beta1 RollbackTo { get; set; }

        /// <summary>
        ///     The annotations to be updated to a deployment
        /// </summary>
        [JsonProperty("updatedAnnotations", NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, string> UpdatedAnnotations { get; set; } = new Dictionary<string, string>();
    }
}
