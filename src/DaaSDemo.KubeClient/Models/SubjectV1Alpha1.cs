using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     Subject contains a reference to the object or user identities a role binding applies to.  This can either hold a direct API object reference, or a value for non-objects such as user and group names.
    /// </summary>
    public class SubjectV1Alpha1 : KubeResource
    {
        /// <summary>
        ///     Name of the object being referenced.
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        ///     Namespace of the referenced object.  If the object kind is non-namespace, such as "User" or "Group", and this value is not empty the Authorizer should report an error.
        /// </summary>
        [JsonProperty("namespace")]
        public string Namespace { get; set; }
    }
}
