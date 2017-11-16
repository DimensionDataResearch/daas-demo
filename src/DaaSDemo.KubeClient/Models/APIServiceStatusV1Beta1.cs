using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     APIServiceStatus contains derived information about an API server
    /// </summary>
    public class APIServiceStatusV1Beta1
    {
        /// <summary>
        ///     Current service state of apiService.
        /// </summary>
        [JsonProperty("conditions", NullValueHandling = NullValueHandling.Ignore)]
        public List<APIServiceConditionV1Beta1> Conditions { get; set; } = new List<APIServiceConditionV1Beta1>();
    }
}
