using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     Represents a Glusterfs mount that lasts the lifetime of a pod. Glusterfs volumes do not support ownership management or SELinux relabeling.
    /// </summary>
    public class GlusterfsVolumeSourceV1
    {
        /// <summary>
        ///     EndpointsName is the endpoint name that details Glusterfs topology. More info: https://releases.k8s.io/HEAD/examples/volumes/glusterfs/README.md#create-a-pod
        /// </summary>
        [JsonProperty("endpoints")]
        public string Endpoints { get; set; }

        /// <summary>
        ///     Path is the Glusterfs volume path. More info: https://releases.k8s.io/HEAD/examples/volumes/glusterfs/README.md#create-a-pod
        /// </summary>
        [JsonProperty("path")]
        public string Path { get; set; }
    }
}
