using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     SubjectAccessReview checks whether or not a user or group can perform an action.
    /// </summary>
    public class SubjectAccessReviewV1Beta1 : KubeResource
    {
        /// <summary>
        ///     Description not provided.
        /// </summary>
        [JsonProperty("metadata")]
        public ObjectMetaV1 Metadata { get; set; }

        /// <summary>
        ///     Spec holds information about the request being evaluated
        /// </summary>
        [JsonProperty("spec")]
        public SubjectAccessReviewSpecV1Beta1 Spec { get; set; }

        /// <summary>
        ///     Status is filled in by the server and indicates whether the request is allowed or not
        /// </summary>
        [JsonProperty("status")]
        public SubjectAccessReviewStatusV1Beta1 Status { get; set; }
    }
}
