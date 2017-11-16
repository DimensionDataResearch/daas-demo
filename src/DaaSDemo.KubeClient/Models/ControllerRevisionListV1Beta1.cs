using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     ControllerRevisionList is a resource containing a list of ControllerRevision objects.
    /// </summary>
    public class ControllerRevisionListV1Beta1 : KubeResource
    {
        /// <summary>
        ///     More info: https://git.k8s.io/community/contributors/devel/api-conventions.md#metadata
        /// </summary>
        [JsonProperty("metadata")]
        public ListMetaV1 Metadata { get; set; }

        /// <summary>
        ///     Items is the list of ControllerRevisions
        /// </summary>
        [JsonProperty("items", NullValueHandling = NullValueHandling.Ignore)]
        public List<ControllerRevisionV1Beta1> Items { get; set; } = new List<ControllerRevisionV1Beta1>();
    }
}
