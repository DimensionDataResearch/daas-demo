using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     ReplicaSetSpec is the specification of a ReplicaSet.
    /// </summary>
    public class ReplicaSetSpecV1Beta1
    {
        /// <summary>
        ///     Minimum number of seconds for which a newly created pod should be ready without any of its container crashing, for it to be considered available. Defaults to 0 (pod will be considered available as soon as it is ready)
        /// </summary>
        [JsonProperty("minReadySeconds")]
        public int MinReadySeconds { get; set; }

        /// <summary>
        ///     Replicas is the number of desired replicas. This is a pointer to distinguish between explicit zero and unspecified. Defaults to 1. More info: https://kubernetes.io/docs/concepts/workloads/controllers/replicationcontroller/#what-is-a-replicationcontroller
        /// </summary>
        [JsonProperty("replicas")]
        public int Replicas { get; set; }

        /// <summary>
        ///     Selector is a label query over pods that should match the replica count. If the selector is empty, it is defaulted to the labels present on the pod template. Label keys and values that must match in order to be controlled by this replica set. More info: https://kubernetes.io/docs/concepts/overview/working-with-objects/labels/#label-selectors
        /// </summary>
        [JsonProperty("selector")]
        public LabelSelectorV1 Selector { get; set; }
    }
}
