using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     EnvVarSource represents a source for the value of an EnvVar.
    /// </summary>
    public class EnvVarSourceV1
    {
        /// <summary>
        ///     Selects a key of a ConfigMap.
        /// </summary>
        [JsonProperty("configMapKeyRef")]
        public ConfigMapKeySelectorV1 ConfigMapKeyRef { get; set; }

        /// <summary>
        ///     Selects a field of the pod: supports metadata.name, metadata.namespace, metadata.labels, metadata.annotations, spec.nodeName, spec.serviceAccountName, status.hostIP, status.podIP.
        /// </summary>
        [JsonProperty("fieldRef")]
        public ObjectFieldSelectorV1 FieldRef { get; set; }

        /// <summary>
        ///     Selects a resource of the container: only resources limits and requests (limits.cpu, limits.memory, requests.cpu and requests.memory) are currently supported.
        /// </summary>
        [JsonProperty("resourceFieldRef")]
        public ResourceFieldSelectorV1 ResourceFieldRef { get; set; }
    }
}
