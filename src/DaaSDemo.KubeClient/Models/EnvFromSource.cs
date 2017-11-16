using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     EnvFromSource represents the source of a set of ConfigMaps
    /// </summary>
    public class EnvFromSourceV1
    {
        /// <summary>
        ///     The ConfigMap to select from
        /// </summary>
        [JsonProperty("configMapRef")]
        public ConfigMapEnvSourceV1 ConfigMapRef { get; set; }

        /// <summary>
        ///     An optional identifer to prepend to each key in the ConfigMap. Must be a C_IDENTIFIER.
        /// </summary>
        [JsonProperty("prefix")]
        public string Prefix { get; set; }
    }
}
