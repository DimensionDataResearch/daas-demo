using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     Pod Security Policy Spec defines the policy enforced.
    /// </summary>
    public class PodSecurityPolicySpecV1Beta1
    {
        /// <summary>
        ///     AllowedCapabilities is a list of capabilities that can be requested to add to the container. Capabilities in this field may be added at the pod author's discretion. You must not list a capability in both AllowedCapabilities and RequiredDropCapabilities.
        /// </summary>
        [JsonProperty("allowedCapabilities")]
        public List<string> AllowedCapabilities { get; set; }

        /// <summary>
        ///     DefaultAddCapabilities is the default set of capabilities that will be added to the container unless the pod spec specifically drops the capability.  You may not list a capabiility in both DefaultAddCapabilities and RequiredDropCapabilities.
        /// </summary>
        [JsonProperty("defaultAddCapabilities")]
        public List<string> DefaultAddCapabilities { get; set; }

        /// <summary>
        ///     FSGroup is the strategy that will dictate what fs group is used by the SecurityContext.
        /// </summary>
        [JsonProperty("fsGroup")]
        public FSGroupStrategyOptionsV1Beta1 FsGroup { get; set; }

        /// <summary>
        ///     hostIPC determines if the policy allows the use of HostIPC in the pod spec.
        /// </summary>
        [JsonProperty("hostIPC")]
        public bool HostIPC { get; set; }

        /// <summary>
        ///     hostNetwork determines if the policy allows the use of HostNetwork in the pod spec.
        /// </summary>
        [JsonProperty("hostNetwork")]
        public bool HostNetwork { get; set; }

        /// <summary>
        ///     hostPID determines if the policy allows the use of HostPID in the pod spec.
        /// </summary>
        [JsonProperty("hostPID")]
        public bool HostPID { get; set; }

        /// <summary>
        ///     hostPorts determines which host port ranges are allowed to be exposed.
        /// </summary>
        [JsonProperty("hostPorts")]
        public List<HostPortRangeV1Beta1> HostPorts { get; set; }

        /// <summary>
        ///     privileged determines if a pod can request to be run as privileged.
        /// </summary>
        [JsonProperty("privileged")]
        public bool Privileged { get; set; }

        /// <summary>
        ///     ReadOnlyRootFilesystem when set to true will force containers to run with a read only root file system.  If the container specifically requests to run with a non-read only root file system the PSP should deny the pod. If set to false the container may run with a read only root file system if it wishes but it will not be forced to.
        /// </summary>
        [JsonProperty("readOnlyRootFilesystem")]
        public bool ReadOnlyRootFilesystem { get; set; }

        /// <summary>
        ///     RequiredDropCapabilities are the capabilities that will be dropped from the container.  These are required to be dropped and cannot be added.
        /// </summary>
        [JsonProperty("requiredDropCapabilities")]
        public List<string> RequiredDropCapabilities { get; set; }

        /// <summary>
        ///     runAsUser is the strategy that will dictate the allowable RunAsUser values that may be set.
        /// </summary>
        [JsonProperty("runAsUser")]
        public RunAsUserStrategyOptionsV1Beta1 RunAsUser { get; set; }

        /// <summary>
        ///     seLinux is the strategy that will dictate the allowable labels that may be set.
        /// </summary>
        [JsonProperty("seLinux")]
        public SELinuxStrategyOptionsV1Beta1 SeLinux { get; set; }

        /// <summary>
        ///     SupplementalGroups is the strategy that will dictate what supplemental groups are used by the SecurityContext.
        /// </summary>
        [JsonProperty("supplementalGroups")]
        public SupplementalGroupsStrategyOptionsV1Beta1 SupplementalGroups { get; set; }
    }
}
