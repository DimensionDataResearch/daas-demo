using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     ServiceAccountList is a list of ServiceAccount objects
    /// </summary>
    public class ServiceAccountListV1 : KubeResourceListV1
    {
        /// <summary>
        ///     List of ServiceAccounts. More info: https://kubernetes.io/docs/tasks/configure-pod-container/configure-service-account/
        /// </summary>
        [JsonProperty("items", NullValueHandling = NullValueHandling.Ignore)]
        public List<ServiceAccountV1> Items { get; set; } = new List<ServiceAccountV1>();
    }
}
