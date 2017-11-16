using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     PodPresetList is a list of PodPreset objects.
    /// </summary>
    public class PodPresetListV1Alpha1 : KubeResource
    {
        /// <summary>
        ///     Standard list metadata. More info: https://git.k8s.io/community/contributors/devel/api-conventions.md#metadata
        /// </summary>
        [JsonProperty("metadata")]
        public ListMetaV1 Metadata { get; set; }

        /// <summary>
        ///     Items is a list of schema objects.
        /// </summary>
        [JsonProperty("items", NullValueHandling = NullValueHandling.Ignore)]
        public List<PodPresetV1Alpha1> Items { get; set; } = new List<PodPresetV1Alpha1>();
    }
}
