using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     PodDisruptionBudgetStatus represents information about the status of a PodDisruptionBudget. Status may trail the actual state of a system.
    /// </summary>
    public class PodDisruptionBudgetStatusV1Beta1
    {
        /// <summary>
        ///     Number of pod disruptions that are currently allowed.
        /// </summary>
        [JsonProperty("disruptionsAllowed")]
        public int DisruptionsAllowed { get; set; }

        /// <summary>
        ///     Most recent generation observed when updating this PDB status. PodDisruptionsAllowed and other status informatio is valid only if observedGeneration equals to PDB's object generation.
        /// </summary>
        [JsonProperty("observedGeneration")]
        public int ObservedGeneration { get; set; }

        /// <summary>
        ///     DisruptedPods contains information about pods whose eviction was processed by the API server eviction subresource handler but has not yet been observed by the PodDisruptionBudget controller. A pod will be in this map from the time when the API server processed the eviction request to the time when the pod is seen by PDB controller as having been marked for deletion (or after a timeout). The key in the map is the name of the pod and the value is the time when the API server processed the eviction request. If the deletion didn't occur and a pod is still there it will be removed from the list automatically by PodDisruptionBudget controller after some time. If everything goes smooth this map should be empty for the most of the time. Large number of entries in the map may indicate problems with pod deletions.
        /// </summary>
        [JsonProperty("disruptedPods", NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, DateTime> DisruptedPods { get; set; } = new Dictionary<string, DateTime>();

        /// <summary>
        ///     total number of pods counted by this disruption budget
        /// </summary>
        [JsonProperty("expectedPods")]
        public int ExpectedPods { get; set; }

        /// <summary>
        ///     current number of healthy pods
        /// </summary>
        [JsonProperty("currentHealthy")]
        public int CurrentHealthy { get; set; }

        /// <summary>
        ///     minimum desired number of healthy pods
        /// </summary>
        [JsonProperty("desiredHealthy")]
        public int DesiredHealthy { get; set; }
    }
}
