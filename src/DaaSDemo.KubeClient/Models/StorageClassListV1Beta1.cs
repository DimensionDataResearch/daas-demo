using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     StorageClassList is a collection of storage classes.
    /// </summary>
    public class StorageClassListV1Beta1 : KubeResource
    {
        /// <summary>
        ///     Standard list metadata More info: https://git.k8s.io/community/contributors/devel/api-conventions.md#metadata
        /// </summary>
        [JsonProperty("metadata")]
        public ListMetaV1 Metadata { get; set; }

        /// <summary>
        ///     Items is the list of StorageClasses
        /// </summary>
        [JsonProperty("items", NullValueHandling = NullValueHandling.Ignore)]
        public List<StorageClassV1Beta1> Items { get; set; } = new List<StorageClassV1Beta1>();
    }
}
