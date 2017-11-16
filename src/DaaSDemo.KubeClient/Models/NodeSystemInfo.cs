using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     NodeSystemInfo is a set of ids/uuids to uniquely identify the node.
    /// </summary>
    public class NodeSystemInfoV1
    {
        /// <summary>
        ///     The Architecture reported by the node
        /// </summary>
        [JsonProperty("architecture")]
        public string Architecture { get; set; }

        /// <summary>
        ///     Boot ID reported by the node.
        /// </summary>
        [JsonProperty("bootID")]
        public string BootID { get; set; }

        /// <summary>
        ///     ContainerRuntime Version reported by the node through runtime remote API (e.g. docker://1.5.0).
        /// </summary>
        [JsonProperty("containerRuntimeVersion")]
        public string ContainerRuntimeVersion { get; set; }

        /// <summary>
        ///     Kernel Version reported by the node from 'uname -r' (e.g. 3.16.0-0.bpo.4-amd64).
        /// </summary>
        [JsonProperty("kernelVersion")]
        public string KernelVersion { get; set; }

        /// <summary>
        ///     KubeProxy Version reported by the node.
        /// </summary>
        [JsonProperty("kubeProxyVersion")]
        public string KubeProxyVersion { get; set; }

        /// <summary>
        ///     Kubelet Version reported by the node.
        /// </summary>
        [JsonProperty("kubeletVersion")]
        public string KubeletVersion { get; set; }

        /// <summary>
        ///     MachineID reported by the node. For unique machine identification in the cluster this field is preferred. Learn more from man(5) machine-id: http://man7.org/linux/man-pages/man5/machine-id.5.html
        /// </summary>
        [JsonProperty("machineID")]
        public string MachineID { get; set; }

        /// <summary>
        ///     The Operating System reported by the node
        /// </summary>
        [JsonProperty("operatingSystem")]
        public string OperatingSystem { get; set; }

        /// <summary>
        ///     OS Image reported by the node from /etc/os-release (e.g. Debian GNU/Linux 7 (wheezy)).
        /// </summary>
        [JsonProperty("osImage")]
        public string OsImage { get; set; }
    }
}
