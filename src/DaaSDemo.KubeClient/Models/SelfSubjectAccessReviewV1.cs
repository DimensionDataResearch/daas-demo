using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     SelfSubjectAccessReview checks whether or the current user can perform an action.  Not filling in a spec.namespace means "in all namespaces".  Self is a special case, because users should always be able to check whether they can perform an action
    /// </summary>
    public class SelfSubjectAccessReviewV1 : KubeResourceV1
    {
        /// <summary>
        ///     Spec holds information about the request being evaluated.  user and groups must be empty
        /// </summary>
        [JsonProperty("spec")]
        public SelfSubjectAccessReviewSpecV1 Spec { get; set; }

        /// <summary>
        ///     Status is filled in by the server and indicates whether the request is allowed or not
        /// </summary>
        [JsonProperty("status")]
        public SubjectAccessReviewStatusV1 Status { get; set; }
    }
}
