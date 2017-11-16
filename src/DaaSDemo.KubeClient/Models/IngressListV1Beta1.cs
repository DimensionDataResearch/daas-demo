using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     IngressList is a collection of Ingress.
    /// </summary>
    public class IngressListV1Beta1 : KubeResourceListV1
    {
        /// <summary>
        ///     Items is the list of Ingress.
        /// </summary>
        [JsonProperty("items", NullValueHandling = NullValueHandling.Ignore)]
        public List<IngressV1Beta1> Items { get; set; } = new List<IngressV1Beta1>();
    }
}
