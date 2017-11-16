using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     Describe a container image
    /// </summary>
    public class ContainerImageV1
    {
        /// <summary>
        ///     Names by which this image is known. e.g. ["gcr.io/google_containers/hyperkube:v1.0.7", "dockerhub.io/google_containers/hyperkube:v1.0.7"]
        /// </summary>
        [JsonProperty("names")]
        public List<string> Names { get; set; }
    }
}
