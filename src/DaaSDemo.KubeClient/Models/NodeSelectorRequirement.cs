using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     A node selector requirement is a selector that contains values, a key, and an operator that relates the key and values.
    /// </summary>
    public class NodeSelectorRequirementV1
    {
        /// <summary>
        ///     The label key that the selector applies to.
        /// </summary>
        [JsonProperty("key")]
        public string Key { get; set; }

        /// <summary>
        ///     Represents a key's relationship to a set of values. Valid operators are In, NotIn, Exists, DoesNotExist. Gt, and Lt.
        /// </summary>
        [JsonProperty("operator")]
        public string Operator { get; set; }
    }
}
