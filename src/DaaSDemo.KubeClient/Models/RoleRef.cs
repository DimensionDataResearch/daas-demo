using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     RoleRef contains information that points to the role being used
    /// </summary>
    public class RoleRefV1Beta1
    {
        /// <summary>
        ///     APIGroup is the group for the resource being referenced
        /// </summary>
        [JsonProperty("apiGroup")]
        public string ApiGroup { get; set; }

        /// <summary>
        ///     Kind is the type of resource being referenced
        /// </summary>
        [JsonProperty("kind")]
        public string Kind { get; set; }
    }
}
