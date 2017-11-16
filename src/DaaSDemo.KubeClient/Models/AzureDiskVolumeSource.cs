using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     AzureDisk represents an Azure Data Disk mount on the host and bind mount to the pod.
    /// </summary>
    public class AzureDiskVolumeSourceV1
    {
        /// <summary>
        ///     Host Caching mode: None, Read Only, Read Write.
        /// </summary>
        [JsonProperty("cachingMode")]
        public string CachingMode { get; set; }

        /// <summary>
        ///     The Name of the data disk in the blob storage
        /// </summary>
        [JsonProperty("diskName")]
        public string DiskName { get; set; }

        /// <summary>
        ///     The URI the data disk in the blob storage
        /// </summary>
        [JsonProperty("diskURI")]
        public string DiskURI { get; set; }

        /// <summary>
        ///     Filesystem type to mount. Must be a filesystem type supported by the host operating system. Ex. "ext4", "xfs", "ntfs". Implicitly inferred to be "ext4" if unspecified.
        /// </summary>
        [JsonProperty("fsType")]
        public string FsType { get; set; }

        /// <summary>
        ///     Expected values Shared: mulitple blob disks per storage account  Dedicated: single blob disk per storage account  Managed: azure managed data disk (only in managed availability set). defaults to shared
        /// </summary>
        [JsonProperty("kind")]
        public string Kind { get; set; }
    }
}
