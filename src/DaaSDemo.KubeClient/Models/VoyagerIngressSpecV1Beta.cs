using KubeNET.Swagger.Model;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace DaaSDemo.KubeClient.Models
{
    [DataContract]
    public class VoyagerIngressSpecV1Beta1
    {
        [DataMember(Name = "tls", EmitDefaultValue = false)]
        public List<V1beta1IngressTLS> Tls { get; set; }

        [DataMember(Name = "rules", EmitDefaultValue = false)]
        public List<VoyagerIngressRuleV1Beta1> Rules { get; set; }
    }
}
