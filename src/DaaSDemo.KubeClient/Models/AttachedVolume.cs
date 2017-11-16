using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     AttachedVolume describes a volume attached to a node
    /// </summary>
    public class AttachedVolumeV1
    {
        /// <summary>
        ///     DevicePath represents the device path where the volume should be available
        /// </summary>
        [JsonProperty("devicePath")]
        public string DevicePath { get; set; }
    }
}
