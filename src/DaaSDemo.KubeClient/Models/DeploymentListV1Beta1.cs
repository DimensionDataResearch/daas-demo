using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     DeploymentList is a list of Deployments.
    /// </summary>
    public class DeploymentListV1Beta1 : KubeResource
    {
        /// <summary>
        ///     Standard list metadata.
        /// </summary>
        [JsonProperty("metadata")]
        public ListMetaV1 Metadata { get; set; }

        /// <summary>
        ///     Items is the list of Deployments.
        /// </summary>
        [JsonProperty("items", NullValueHandling = NullValueHandling.Ignore)]
        public List<DeploymentV1Beta1> Items { get; set; } = new List<DeploymentV1Beta1>();
    }
}
