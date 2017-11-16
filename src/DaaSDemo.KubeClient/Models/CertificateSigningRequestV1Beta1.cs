using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     Describes a certificate signing request
    /// </summary>
    public class CertificateSigningRequestV1Beta1 : KubeResourceV1
    {
        /// <summary>
        ///     The certificate request itself and any additional information.
        /// </summary>
        [JsonProperty("spec")]
        public CertificateSigningRequestSpecV1Beta1 Spec { get; set; }

        /// <summary>
        ///     Derived information about the request.
        /// </summary>
        [JsonProperty("status")]
        public CertificateSigningRequestStatusV1Beta1 Status { get; set; }
    }
}
