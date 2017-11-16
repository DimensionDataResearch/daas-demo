using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     PodStatus represents information about the status of a pod. Status may trail the actual state of a system.
    /// </summary>
    public class PodStatusV1
    {
        /// <summary>
        ///     Current service state of pod. More info: https://kubernetes.io/docs/concepts/workloads/pods/pod-lifecycle#pod-conditions
        /// </summary>
        [JsonProperty("conditions")]
        public List<PodConditionV1> Conditions { get; set; }

        /// <summary>
        ///     The list has one entry per container in the manifest. Each entry is currently the output of `docker inspect`. More info: https://kubernetes.io/docs/concepts/workloads/pods/pod-lifecycle#pod-and-container-status
        /// </summary>
        [JsonProperty("containerStatuses")]
        public List<ContainerStatusV1> ContainerStatuses { get; set; }

        /// <summary>
        ///     IP address of the host to which the pod is assigned. Empty if not yet scheduled.
        /// </summary>
        [JsonProperty("hostIP")]
        public string HostIP { get; set; }

        /// <summary>
        ///     The list has one entry per init container in the manifest. The most recent successful init container will have ready = true, the most recently started container will have startTime set. More info: https://kubernetes.io/docs/concepts/workloads/pods/pod-lifecycle#pod-and-container-status
        /// </summary>
        [JsonProperty("initContainerStatuses")]
        public List<ContainerStatusV1> InitContainerStatuses { get; set; }

        /// <summary>
        ///     A human readable message indicating details about why the pod is in this condition.
        /// </summary>
        [JsonProperty("message")]
        public string Message { get; set; }

        /// <summary>
        ///     Current condition of the pod. More info: https://kubernetes.io/docs/concepts/workloads/pods/pod-lifecycle#pod-phase
        /// </summary>
        [JsonProperty("phase")]
        public string Phase { get; set; }

        /// <summary>
        ///     IP address allocated to the pod. Routable at least within the cluster. Empty if not yet allocated.
        /// </summary>
        [JsonProperty("podIP")]
        public string PodIP { get; set; }

        /// <summary>
        ///     The Quality of Service (QOS) classification assigned to the pod based on resource requirements See PodQOSClass type for available QOS classes More info: https://github.com/kubernetes/kubernetes/blob/master/docs/design/resource-qos.md
        /// </summary>
        [JsonProperty("qosClass")]
        public string QosClass { get; set; }

        /// <summary>
        ///     A brief CamelCase message indicating details about why the pod is in this state. e.g. 'OutOfDisk'
        /// </summary>
        [JsonProperty("reason")]
        public string Reason { get; set; }
    }
}
