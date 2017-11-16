using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     LocalSubjectAccessReview checks whether or not a user or group can perform an action in a given namespace. Having a namespace scoped resource makes it much easier to grant namespace scoped policy that includes permissions checking.
    /// </summary>
    public class LocalSubjectAccessReviewV1 : KubeResource
    {
        /// <summary>
        ///     Description not provided.
        /// </summary>
        [JsonProperty("metadata")]
        public ObjectMetaV1 Metadata { get; set; }

        /// <summary>
        ///     Spec holds information about the request being evaluated.  spec.namespace must be equal to the namespace you made the request against.  If empty, it is defaulted.
        /// </summary>
        [JsonProperty("spec")]
        public SubjectAccessReviewSpecV1 Spec { get; set; }

        /// <summary>
        ///     Status is filled in by the server and indicates whether the request is allowed or not
        /// </summary>
        [JsonProperty("status")]
        public SubjectAccessReviewStatusV1 Status { get; set; }
    }
}
