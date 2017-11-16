using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     Info contains versioning information. how we'll want to distribute that information.
    /// </summary>
    public class InfoVersion
    {
        /// <summary>
        ///     No description provided.
        /// </summary>
        [JsonProperty("buildDate")]
        public string BuildDate { get; set; }

        /// <summary>
        ///     No description provided.
        /// </summary>
        [JsonProperty("compiler")]
        public string Compiler { get; set; }

        /// <summary>
        ///     No description provided.
        /// </summary>
        [JsonProperty("gitCommit")]
        public string GitCommit { get; set; }

        /// <summary>
        ///     No description provided.
        /// </summary>
        [JsonProperty("gitTreeState")]
        public string GitTreeState { get; set; }

        /// <summary>
        ///     No description provided.
        /// </summary>
        [JsonProperty("gitVersion")]
        public string GitVersion { get; set; }

        /// <summary>
        ///     No description provided.
        /// </summary>
        [JsonProperty("goVersion")]
        public string GoVersion { get; set; }

        /// <summary>
        ///     No description provided.
        /// </summary>
        [JsonProperty("major")]
        public string Major { get; set; }

        /// <summary>
        ///     No description provided.
        /// </summary>
        [JsonProperty("minor")]
        public string Minor { get; set; }
    }
}
