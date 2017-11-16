using KubeNET.Swagger.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace DaaSDemo.KubeClient.Models
{
    public class ResourceEventV1<TResource>
    {
        [JsonProperty("type")]
        public string EventType { get; set; }

        [JsonProperty("object")]
        public TResource Resource { get; set; }
    }
}
