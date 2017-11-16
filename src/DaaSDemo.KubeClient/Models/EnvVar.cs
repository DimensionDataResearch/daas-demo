using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     EnvVar represents an environment variable present in a Container.
    /// </summary>
    public class EnvVarV1
    {
        /// <summary>
        ///     Name of the environment variable. Must be a C_IDENTIFIER.
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        ///     Variable references $(VAR_NAME) are expanded using the previous defined environment variables in the container and any service environment variables. If a variable cannot be resolved, the reference in the input string will be unchanged. The $(VAR_NAME) syntax can be escaped with a double $$, ie: $$(VAR_NAME). Escaped references will never be expanded, regardless of whether the variable exists or not. Defaults to "".
        /// </summary>
        [JsonProperty("value")]
        public string Value { get; set; }
    }
}
