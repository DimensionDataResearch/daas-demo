using KubeNET.Swagger.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace DaaSDemo.KubeClient.Models
{
    [DataContract]
    public class V1ResourceEvent<TResource>
    {
        [DataMember(Name = "type")]
        public string EventType { get; set; }

        [DataMember(Name = "object")]
        public TResource Resource { get; set; }
    }
}
