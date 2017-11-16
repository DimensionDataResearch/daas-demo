using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     PersistentVolumeList is a list of PersistentVolume items.
    /// </summary>
    public class PersistentVolumeListV1 : KubeResource
    {
        /// <summary>
        ///     Standard list metadata. More info: https://git.k8s.io/community/contributors/devel/api-conventions.md#types-kinds
        /// </summary>
        [JsonProperty("metadata")]
        public ListMetaV1 Metadata { get; set; }

        /// <summary>
        ///     List of persistent volumes. More info: https://kubernetes.io/docs/concepts/storage/persistent-volumes
        /// </summary>
        [JsonProperty("items", NullValueHandling = NullValueHandling.Ignore)]
        public List<PersistentVolumeV1> Items { get; set; } = new List<PersistentVolumeV1>();
    }
}
