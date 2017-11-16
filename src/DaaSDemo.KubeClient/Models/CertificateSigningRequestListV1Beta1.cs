using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     No description provided.
    /// </summary>
    public class CertificateSigningRequestListV1Beta1 : KubeResourceListV1
    {
        /// <summary>
        ///     Description not provided.
        /// </summary>
        [JsonProperty("items", NullValueHandling = NullValueHandling.Ignore)]
        public List<CertificateSigningRequestV1Beta1> Items { get; set; } = new List<CertificateSigningRequestV1Beta1>();
    }
}
