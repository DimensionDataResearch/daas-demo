using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     SubjectAccessReviewSpec is a description of the access request.  Exactly one of ResourceAuthorizationAttributes and NonResourceAuthorizationAttributes must be set
    /// </summary>
    public class SubjectAccessReviewSpecV1Beta1
    {
        /// <summary>
        ///     Extra corresponds to the user.Info.GetExtra() method from the authenticator.  Since that is input to the authorizer it needs a reflection here.
        /// </summary>
        [JsonProperty("extra")]
        public Dictionary<string, string> Extra { get; set; }

        /// <summary>
        ///     Groups is the groups you're testing for.
        /// </summary>
        [JsonProperty("group")]
        public List<string> Group { get; set; }

        /// <summary>
        ///     NonResourceAttributes describes information for a non-resource access request
        /// </summary>
        [JsonProperty("nonResourceAttributes")]
        public NonResourceAttributesV1Beta1 NonResourceAttributes { get; set; }

        /// <summary>
        ///     ResourceAuthorizationAttributes describes information for a resource access request
        /// </summary>
        [JsonProperty("resourceAttributes")]
        public ResourceAttributesV1Beta1 ResourceAttributes { get; set; }
    }
}
