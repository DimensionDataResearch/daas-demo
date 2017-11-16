using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     ContainerStateWaiting is a waiting state of a container.
    /// </summary>
    public class ContainerStateWaitingV1
    {
        /// <summary>
        ///     Message regarding why the container is not yet running.
        /// </summary>
        [JsonProperty("message")]
        public string Message { get; set; }

        /// <summary>
        ///     (brief) reason the container is not yet running.
        /// </summary>
        [JsonProperty("reason")]
        public string Reason { get; set; }
    }
}
