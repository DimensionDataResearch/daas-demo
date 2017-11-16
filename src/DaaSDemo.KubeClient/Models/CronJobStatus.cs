using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     CronJobStatus represents the current state of a cron job.
    /// </summary>
    public class CronJobStatusV2Alpha1
    {
        /// <summary>
        ///     A list of pointers to currently running jobs.
        /// </summary>
        [JsonProperty("active")]
        public List<ObjectReferenceV1> Active { get; set; }
    }
}
