using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     PersistentVolumeClaimStatus is the current status of a persistent volume claim.
    /// </summary>
    public class PersistentVolumeClaimStatusV1
    {
        /// <summary>
        ///     AccessModes contains the actual access modes the volume backing the PVC has. More info: https://kubernetes.io/docs/concepts/storage/persistent-volumes#access-modes-1
        /// </summary>
        [JsonProperty("accessModes")]
        public List<string> AccessModes { get; set; }

        /// <summary>
        ///     Represents the actual resources of the underlying volume.
        /// </summary>
        [JsonProperty("capacity")]
        public Dictionary<string, string> Capacity { get; set; }
    }
}
