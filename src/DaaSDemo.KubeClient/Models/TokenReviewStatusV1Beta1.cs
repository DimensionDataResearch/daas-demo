using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     TokenReviewStatus is the result of the token authentication request.
    /// </summary>
    public class TokenReviewStatusV1Beta1
    {
        /// <summary>
        ///     Authenticated indicates that the token was associated with a known user.
        /// </summary>
        [JsonProperty("authenticated")]
        public bool Authenticated { get; set; }

        /// <summary>
        ///     Error indicates that the token couldn't be checked
        /// </summary>
        [JsonProperty("error")]
        public string Error { get; set; }

        /// <summary>
        ///     User is the UserInfo associated with the provided token.
        /// </summary>
        [JsonProperty("user")]
        public UserInfoV1Beta1 User { get; set; }
    }
}
