using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     ServiceReference holds a reference to Service.legacy.k8s.io
    /// </summary>
    public class ServiceReferenceV1Beta1
    {
        /// <summary>
        ///     Name is the name of the service
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }
    }
}
