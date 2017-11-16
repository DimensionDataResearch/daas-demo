using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     Maps a string key to a path within a volume.
    /// </summary>
    public class KeyToPathV1
    {
        /// <summary>
        ///     Optional: mode bits to use on this file, must be a value between 0 and 0777. If not specified, the volume defaultMode will be used. This might be in conflict with other options that affect the file mode, like fsGroup, and the result can be other mode bits set.
        /// </summary>
        [JsonProperty("mode")]
        public int Mode { get; set; }

        /// <summary>
        ///     The relative path of the file to map the key to. May not be an absolute path. May not contain the path element '..'. May not start with the string '..'.
        /// </summary>
        [JsonProperty("path")]
        public string Path { get; set; }

        /// <summary>
        ///     The key to project.
        /// </summary>
        [JsonProperty("key")]
        public string Key { get; set; }
    }
}
