using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     ObjectFieldSelector selects an APIVersioned field of an object.
    /// </summary>
    public class ObjectFieldSelectorV1
    {
        /// <summary>
        ///     Version of the schema the FieldPath is written in terms of, defaults to "v1".
        /// </summary>
        [JsonProperty("apiVersion")]
        public string ApiVersion { get; set; }
    }
}
