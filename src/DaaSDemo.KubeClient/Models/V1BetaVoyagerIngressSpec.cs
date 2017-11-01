using KubeNET.Swagger.Model;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace DaaSDemo.KubeClient.Models
{
    [DataContract]
    public class V1Beta1VoyagerIngressSpec
    {
        [DataMember(Name = "tls", EmitDefaultValue = false)]
        public List<V1beta1IngressTLS> Tls { get; set; }

        [DataMember(Name = "rules", EmitDefaultValue = false)]
        public List<V1Beta1VoyagerIngressRule> Rules { get; set; }
    }
}