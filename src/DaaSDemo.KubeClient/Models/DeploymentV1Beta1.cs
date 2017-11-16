using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     Deployment enables declarative updates for Pods and ReplicaSets.
    /// </summary>
    public class DeploymentV1Beta1 : KubeResource
    {
        /// <summary>
        ///     Standard object metadata.
        /// </summary>
        [JsonProperty("metadata")]
        public ObjectMetaV1 Metadata { get; set; }

        /// <summary>
        ///     Specification of the desired behavior of the Deployment.
        /// </summary>
        [JsonProperty("spec")]
        public DeploymentSpecV1Beta1 Spec { get; set; }

        /// <summary>
        ///     Most recently observed status of the Deployment.
        /// </summary>
        [JsonProperty("status")]
        public DeploymentStatusV1Beta1 Status { get; set; }
    }
}
