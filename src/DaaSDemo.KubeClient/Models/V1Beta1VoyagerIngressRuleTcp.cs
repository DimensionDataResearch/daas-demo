using KubeNET.Swagger.Model;
using System.Runtime.Serialization;

namespace DaaSDemo.KubeClient.Models
{
    [DataContract]
    public class V1Beta1VoyagerIngressRuleTcp
    {
        [DataMember(Name = "backend", EmitDefaultValue = false)]
        public V1beta1IngressBackend Backend { get; set; }

        [DataMember(Name = "port", EmitDefaultValue = false)]
        public string Port { get; set; }
    }
}