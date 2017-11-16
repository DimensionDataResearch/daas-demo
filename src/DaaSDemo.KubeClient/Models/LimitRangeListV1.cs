using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     LimitRangeList is a list of LimitRange items.
    /// </summary>
    public class LimitRangeListV1 : KubeResource
    {
        /// <summary>
        ///     Standard list metadata. More info: https://git.k8s.io/community/contributors/devel/api-conventions.md#types-kinds
        /// </summary>
        [JsonProperty("metadata")]
        public ListMetaV1 Metadata { get; set; }

        /// <summary>
        ///     Items is a list of LimitRange objects. More info: https://git.k8s.io/community/contributors/design-proposals/admission_control_limit_range.md
        /// </summary>
        [JsonProperty("items", NullValueHandling = NullValueHandling.Ignore)]
        public List<LimitRangeV1> Items { get; set; } = new List<LimitRangeV1>();
    }
}
