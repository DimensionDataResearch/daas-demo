using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     LocalObjectReference contains enough information to let you locate the referenced object inside the same namespace.
    /// </summary>
    public class LocalObjectReferenceV1
    {
        /// <summary>
        ///     Name of the referent. More info: https://kubernetes.io/docs/concepts/overview/working-with-objects/names/#names
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }
    }
}
