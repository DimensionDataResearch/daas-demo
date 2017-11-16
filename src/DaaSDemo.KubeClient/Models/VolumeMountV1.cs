using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     VolumeMount describes a mounting of a Volume within a container.
    /// </summary>
    public class VolumeMountV1
    {
        /// <summary>
        ///     This must match the Name of a Volume.
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        ///     Path within the container at which the volume should be mounted.  Must not contain ':'.
        /// </summary>
        [JsonProperty("mountPath")]
        public string MountPath { get; set; }

        /// <summary>
        ///     Path within the volume from which the container's volume should be mounted. Defaults to "" (volume's root).
        /// </summary>
        [JsonProperty("subPath")]
        public string SubPath { get; set; }

        /// <summary>
        ///     Mounted read-only if true, read-write otherwise (false or unspecified). Defaults to false.
        /// </summary>
        [JsonProperty("readOnly")]
        public bool ReadOnly { get; set; }
    }
}
