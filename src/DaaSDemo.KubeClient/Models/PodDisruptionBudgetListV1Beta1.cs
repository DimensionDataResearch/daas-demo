using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     PodDisruptionBudgetList is a collection of PodDisruptionBudgets.
    /// </summary>
    public class PodDisruptionBudgetListV1Beta1 : KubeResourceListV1
    {
        /// <summary>
        ///     Description not provided.
        /// </summary>
        [JsonProperty("items", NullValueHandling = NullValueHandling.Ignore)]
        public List<PodDisruptionBudgetV1Beta1> Items { get; set; } = new List<PodDisruptionBudgetV1Beta1>();
    }
}
