using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     A StatefulSetSpec is the specification of a StatefulSet.
    /// </summary>
    public class StatefulSetSpecV1Beta1
    {
        /// <summary>
        ///     serviceName is the name of the service that governs this StatefulSet. This service must exist before the StatefulSet, and is responsible for the network identity of the set. Pods get DNS/hostnames that follow the pattern: pod-specific-string.serviceName.default.svc.cluster.local where "pod-specific-string" is managed by the StatefulSet controller.
        /// </summary>
        [JsonProperty("serviceName")]
        public string ServiceName { get; set; }

        /// <summary>
        ///     template is the object that describes the pod that will be created if insufficient replicas are detected. Each pod stamped out by the StatefulSet will fulfill this Template, but have a unique identity from the rest of the StatefulSet.
        /// </summary>
        [JsonProperty("template")]
        public PodTemplateSpecV1 Template { get; set; }

        /// <summary>
        ///     selector is a label query over pods that should match the replica count. If empty, defaulted to labels on the pod template. More info: https://kubernetes.io/docs/concepts/overview/working-with-objects/labels/#label-selectors
        /// </summary>
        [JsonProperty("selector")]
        public LabelSelectorV1 Selector { get; set; }

        /// <summary>
        ///     replicas is the desired number of replicas of the given Template. These are replicas in the sense that they are instantiations of the same Template, but individual replicas also have a consistent identity. If unspecified, defaults to 1.
        /// </summary>
        [JsonProperty("replicas")]
        public int Replicas { get; set; }

        /// <summary>
        ///     volumeClaimTemplates is a list of claims that pods are allowed to reference. The StatefulSet controller is responsible for mapping network identities to claims in a way that maintains the identity of a pod. Every claim in this list must have at least one matching (by name) volumeMount in one container in the template. A claim in this list takes precedence over any volumes in the template, with the same name.
        /// </summary>
        [JsonProperty("volumeClaimTemplates", NullValueHandling = NullValueHandling.Ignore)]
        public List<PersistentVolumeClaimV1> VolumeClaimTemplates { get; set; } = new List<PersistentVolumeClaimV1>();

        /// <summary>
        ///     revisionHistoryLimit is the maximum number of revisions that will be maintained in the StatefulSet's revision history. The revision history consists of all revisions not represented by a currently applied StatefulSetSpec version. The default value is 10.
        /// </summary>
        [JsonProperty("revisionHistoryLimit")]
        public int RevisionHistoryLimit { get; set; }

        /// <summary>
        ///     podManagementPolicy controls how pods are created during initial scale up, when replacing pods on nodes, or when scaling down. The default policy is `OrderedReady`, where pods are created in increasing order (pod-0, then pod-1, etc) and the controller will wait until each pod is ready before continuing. When scaling down, the pods are removed in the opposite order. The alternative policy is `Parallel` which will create pods in parallel to match the desired scale without waiting, and on scale down will delete all pods at once.
        /// </summary>
        [JsonProperty("podManagementPolicy")]
        public string PodManagementPolicy { get; set; }

        /// <summary>
        ///     updateStrategy indicates the StatefulSetUpdateStrategy that will be employed to update Pods in the StatefulSet when a revision is made to Template.
        /// </summary>
        [JsonProperty("updateStrategy")]
        public StatefulSetUpdateStrategyV1Beta1 UpdateStrategy { get; set; }
    }
}
