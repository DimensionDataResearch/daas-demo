using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     CrossVersionObjectReference contains enough information to let you identify the referred resource.
    /// </summary>
    public class CrossVersionObjectReferenceV1
    {
        /// <summary>
        ///     Kind of the referent; More info: https://git.k8s.io/community/contributors/devel/api-conventions.md#types-kinds"
        /// </summary>
        [JsonProperty("kind")]
        public string Kind { get; set; }

        /// <summary>
        ///     Name of the referent; More info: http://kubernetes.io/docs/user-guide/identifiers#names
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        ///     API version of the referent
        /// </summary>
        [JsonProperty("apiVersion")]
        public string ApiVersion { get; set; }
    }
}
