using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     TokenReview attempts to authenticate a token to a known user. Note: TokenReview requests may be cached by the webhook token authenticator plugin in the kube-apiserver.
    /// </summary>
    public class TokenReviewV1Beta1 : KubeResource
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
        public TokenReviewSpecV1Beta1 Spec { get; set; }

        /// <summary>
        ///     Status is filled in by the server and indicates whether the request can be authenticated.
        /// </summary>
        [JsonProperty("status")]
        public TokenReviewStatusV1Beta1 Status { get; set; }
    }
}
