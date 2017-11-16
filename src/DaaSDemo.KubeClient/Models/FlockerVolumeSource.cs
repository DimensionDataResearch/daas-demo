using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     Represents a Flocker volume mounted by the Flocker agent. One and only one of datasetName and datasetUUID should be set. Flocker volumes do not support ownership management or SELinux relabeling.
    /// </summary>
    public class FlockerVolumeSourceV1
    {
        /// <summary>
        ///     Name of the dataset stored as metadata -> name on the dataset for Flocker should be considered as deprecated
        /// </summary>
        [JsonProperty("datasetName")]
        public string DatasetName { get; set; }
    }
}
