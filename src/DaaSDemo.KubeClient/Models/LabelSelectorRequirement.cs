using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     A label selector requirement is a selector that contains values, a key, and an operator that relates the key and values.
    /// </summary>
    public class LabelSelectorRequirementV1
    {
        /// <summary>
        ///     key is the label key that the selector applies to.
        /// </summary>
        [JsonProperty("key")]
        public string Key { get; set; }

        /// <summary>
        ///     operator represents a key's relationship to a set of values. Valid operators ard In, NotIn, Exists and DoesNotExist.
        /// </summary>
        [JsonProperty("operator")]
        public string Operator { get; set; }
    }
}
