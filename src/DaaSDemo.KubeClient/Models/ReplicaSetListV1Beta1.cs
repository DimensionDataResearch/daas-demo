using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     ReplicaSetList is a collection of ReplicaSets.
    /// </summary>
    public class ReplicaSetListV1Beta1 : KubeResource
    {
        /// <summary>
        ///     Standard list metadata. More info: https://git.k8s.io/community/contributors/devel/api-conventions.md#types-kinds
        /// </summary>
        [JsonProperty("metadata")]
        public ListMetaV1 Metadata { get; set; }

        /// <summary>
        ///     List of ReplicaSets. More info: https://kubernetes.io/docs/concepts/workloads/controllers/replicationcontroller
        /// </summary>
        [JsonProperty("items", NullValueHandling = NullValueHandling.Ignore)]
        public List<ReplicaSetV1Beta1> Items { get; set; } = new List<ReplicaSetV1Beta1>();
    }
}
