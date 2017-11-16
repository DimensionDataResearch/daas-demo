using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     TokenReviewSpec is a description of the token authentication request.
    /// </summary>
    public class TokenReviewSpecV1
    {
        /// <summary>
        ///     Token is the opaque bearer token.
        /// </summary>
        [JsonProperty("token")]
        public string Token { get; set; }
    }
}
