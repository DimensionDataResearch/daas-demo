using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     JobList is a collection of jobs.
    /// </summary>
    public class JobListV1 : KubeResourceListV1
    {
        /// <summary>
        ///     items is the list of Jobs.
        /// </summary>
        [JsonProperty("items", NullValueHandling = NullValueHandling.Ignore)]
        public List<JobV1> Items { get; set; } = new List<JobV1>();
    }
}
