using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     Represents an empty directory for a pod. Empty directory volumes support ownership management and SELinux relabeling.
    /// </summary>
    public class EmptyDirVolumeSourceV1
    {
        /// <summary>
        ///     What type of storage medium should back this directory. The default is "" which means to use the node's default medium. Must be an empty string (default) or Memory. More info: https://kubernetes.io/docs/concepts/storage/volumes#emptydir
        /// </summary>
        [JsonProperty("medium")]
        public string Medium { get; set; }
    }
}
