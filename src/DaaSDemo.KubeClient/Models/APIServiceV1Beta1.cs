using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     APIService represents a server for a particular GroupVersion. Name must be "version.group".
    /// </summary>
    public class APIServiceV1Beta1 : KubeResource
    {
        /// <summary>
        ///     Description not provided.
        /// </summary>
        [JsonProperty("metadata")]
        public ObjectMetaV1 Metadata { get; set; }

        /// <summary>
        ///     Spec contains information for locating and communicating with a server
        /// </summary>
        [JsonProperty("spec")]
        public APIServiceSpecV1Beta1 Spec { get; set; }

        /// <summary>
        ///     Status contains derived information about an API server
        /// </summary>
        [JsonProperty("status")]
        public APIServiceStatusV1Beta1 Status { get; set; }
    }
}
