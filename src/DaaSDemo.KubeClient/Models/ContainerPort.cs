using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     ContainerPort represents a network port in a single container.
    /// </summary>
    public class ContainerPortV1
    {
        /// <summary>
        ///     Number of port to expose on the pod's IP address. This must be a valid port number, 0 < x < 65536.
        /// </summary>
        [JsonProperty("containerPort")]
        public int ContainerPort { get; set; }

        /// <summary>
        ///     What host IP to bind the external port to.
        /// </summary>
        [JsonProperty("hostIP")]
        public string HostIP { get; set; }

        /// <summary>
        ///     Number of port to expose on the host. If specified, this must be a valid port number, 0 < x < 65536. If HostNetwork is specified, this must match ContainerPort. Most containers do not need this.
        /// </summary>
        [JsonProperty("hostPort")]
        public int HostPort { get; set; }

        /// <summary>
        ///     If specified, this must be an IANA_SVC_NAME and unique within the pod. Each named port in a pod must have a unique name. Name for the port that can be referred to by services.
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }
    }
}
