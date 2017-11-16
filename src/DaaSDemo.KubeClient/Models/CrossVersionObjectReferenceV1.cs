using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     CrossVersionObjectReference contains enough information to let you identify the referred resource.
    /// </summary>
    public class CrossVersionObjectReferenceV1 : KubeResource
    {
        /// <summary>
        ///     Name of the referent; More info: http://kubernetes.io/docs/user-guide/identifiers#names
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }
    }
}
