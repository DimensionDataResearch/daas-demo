using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     CronJobList is a collection of cron jobs.
    /// </summary>
    public class CronJobListV2Alpha1 : KubeResourceListV1
    {
        /// <summary>
        ///     items is the list of CronJobs.
        /// </summary>
        [JsonProperty("items", NullValueHandling = NullValueHandling.Ignore)]
        public List<CronJobV2Alpha1> Items { get; set; } = new List<CronJobV2Alpha1>();
    }
}
