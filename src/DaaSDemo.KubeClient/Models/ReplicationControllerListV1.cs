using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     ReplicationControllerList is a collection of replication controllers.
    /// </summary>
    public class ReplicationControllerListV1 : KubeResource
    {
        /// <summary>
        ///     Standard list metadata. More info: https://git.k8s.io/community/contributors/devel/api-conventions.md#types-kinds
        /// </summary>
        [JsonProperty("metadata")]
        public ListMetaV1 Metadata { get; set; }

        /// <summary>
        ///     List of replication controllers. More info: https://kubernetes.io/docs/concepts/workloads/controllers/replicationcontroller
        /// </summary>
        [JsonProperty("items", NullValueHandling = NullValueHandling.Ignore)]
        public List<ReplicationControllerV1> Items { get; set; } = new List<ReplicationControllerV1>();
    }
}
