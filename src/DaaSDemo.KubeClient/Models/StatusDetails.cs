using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     StatusDetails is a set of additional properties that MAY be set by the server to provide additional information about a response. The Reason field of a Status object defines what attributes will be set. Clients must ignore fields that do not match the defined type of each attribute, and should assume that any attribute may be empty, invalid, or under defined.
    /// </summary>
    public class StatusDetailsV1
    {
        /// <summary>
        ///     The Causes array includes more details associated with the StatusReason failure. Not all StatusReasons may provide detailed causes.
        /// </summary>
        [JsonProperty("causes")]
        public List<StatusCauseV1> Causes { get; set; }

        /// <summary>
        ///     The group attribute of the resource associated with the status StatusReason.
        /// </summary>
        [JsonProperty("group")]
        public string Group { get; set; }

        /// <summary>
        ///     The kind attribute of the resource associated with the status StatusReason. On some operations may differ from the requested resource Kind. More info: https://git.k8s.io/community/contributors/devel/api-conventions.md#types-kinds
        /// </summary>
        [JsonProperty("kind")]
        public string Kind { get; set; }

        /// <summary>
        ///     The name attribute of the resource associated with the status StatusReason (when there is a single name which can be described).
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        ///     If specified, the time in seconds before the operation should be retried.
        /// </summary>
        [JsonProperty("retryAfterSeconds")]
        public int RetryAfterSeconds { get; set; }
    }
}
