using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     Event is a report of an event somewhere in the cluster.
    /// </summary>
    public class EventV1
    {
        /// <summary>
        ///     APIVersion defines the versioned schema of this representation of an object. Servers should convert recognized schemas to the latest internal value, and may reject unrecognized values. More info: https://git.k8s.io/community/contributors/devel/api-conventions.md#resources
        /// </summary>
        [JsonProperty("apiVersion")]
        public string ApiVersion { get; set; }

        /// <summary>
        ///     The number of times this event has occurred.
        /// </summary>
        [JsonProperty("count")]
        public int Count { get; set; }

        /// <summary>
        ///     The time at which the event was first recorded. (Time of server receipt is in TypeMeta.)
        /// </summary>
        [JsonProperty("firstTimestamp")]
        public DateTime FirstTimestamp { get; set; }

        /// <summary>
        ///     The object that this event is about.
        /// </summary>
        [JsonProperty("involvedObject")]
        public ObjectReferenceV1 InvolvedObject { get; set; }

        /// <summary>
        ///     Kind is a string value representing the REST resource this object represents. Servers may infer this from the endpoint the client submits requests to. Cannot be updated. In CamelCase. More info: https://git.k8s.io/community/contributors/devel/api-conventions.md#types-kinds
        /// </summary>
        [JsonProperty("kind")]
        public string Kind { get; set; }

        /// <summary>
        ///     The time at which the most recent occurrence of this event was recorded.
        /// </summary>
        [JsonProperty("lastTimestamp")]
        public DateTime LastTimestamp { get; set; }

        /// <summary>
        ///     A human-readable description of the status of this operation.
        /// </summary>
        [JsonProperty("message")]
        public string Message { get; set; }

        /// <summary>
        ///     Standard object's metadata. More info: https://git.k8s.io/community/contributors/devel/api-conventions.md#metadata
        /// </summary>
        [JsonProperty("metadata")]
        public ObjectMetaV1 Metadata { get; set; }

        /// <summary>
        ///     This should be a short, machine understandable string that gives the reason for the transition into the object's current status.
        /// </summary>
        [JsonProperty("reason")]
        public string Reason { get; set; }

        /// <summary>
        ///     The component reporting this event. Should be a short machine understandable string.
        /// </summary>
        [JsonProperty("source")]
        public EventSourceV1 Source { get; set; }
    }
}
