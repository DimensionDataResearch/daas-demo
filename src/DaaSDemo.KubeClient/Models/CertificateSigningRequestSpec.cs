using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     This information is immutable after the request is created. Only the Request and Usages fields can be set on creation, other fields are derived by Kubernetes and cannot be modified by users.
    /// </summary>
    public class CertificateSigningRequestSpecV1Beta1
    {
        /// <summary>
        ///     Extra information about the requesting user. See user.Info interface for details.
        /// </summary>
        [JsonProperty("extra")]
        public Dictionary<string, string> Extra { get; set; }

        /// <summary>
        ///     Group information about the requesting user. See user.Info interface for details.
        /// </summary>
        [JsonProperty("groups")]
        public List<string> Groups { get; set; }

        /// <summary>
        ///     Base64-encoded PKCS#10 CSR data
        /// </summary>
        [JsonProperty("request")]
        public string Request { get; set; }

        /// <summary>
        ///     UID information about the requesting user. See user.Info interface for details.
        /// </summary>
        [JsonProperty("uid")]
        public string Uid { get; set; }

        /// <summary>
        ///     allowedUsages specifies a set of usage contexts the key will be valid for. See: https://tools.ietf.org/html/rfc5280#section-4.2.1.3
        ///          https://tools.ietf.org/html/rfc5280#section-4.2.1.12
        /// </summary>
        [JsonProperty("usages")]
        public List<string> Usages { get; set; }
    }
}
