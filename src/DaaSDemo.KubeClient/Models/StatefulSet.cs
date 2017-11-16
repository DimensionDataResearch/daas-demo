using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     StatefulSet represents a set of pods with consistent identities. Identities are defined as:
    ///      - Network: A single stable DNS and hostname.
    ///      - Storage: As many VolumeClaims as requested.
    ///     The StatefulSet guarantees that a given network identity will always map to the same storage identity.
    /// </summary>
    public class StatefulSetV1Beta1
    {
        /// <summary>
        ///     APIVersion defines the versioned schema of this representation of an object. Servers should convert recognized schemas to the latest internal value, and may reject unrecognized values. More info: https://git.k8s.io/community/contributors/devel/api-conventions.md#resources
        /// </summary>
        [JsonProperty("apiVersion")]
        public string ApiVersion { get; set; }

        /// <summary>
        ///     Kind is a string value representing the REST resource this object represents. Servers may infer this from the endpoint the client submits requests to. Cannot be updated. In CamelCase. More info: https://git.k8s.io/community/contributors/devel/api-conventions.md#types-kinds
        /// </summary>
        [JsonProperty("kind")]
        public string Kind { get; set; }

        /// <summary>
        ///     No description provided.
        /// </summary>
        [JsonProperty("metadata")]
        public ObjectMetaV1 Metadata { get; set; }

        /// <summary>
        ///     Spec defines the desired identities of pods in this set.
        /// </summary>
        [JsonProperty("spec")]
        public StatefulSetSpecV1Beta1 Spec { get; set; }
    }
}
