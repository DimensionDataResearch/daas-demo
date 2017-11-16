using KubeNET.Swagger.Model;
using System.Runtime.Serialization;

namespace DaaSDemo.KubeClient.Models
{
    [DataContract]
    public class VoyagerIngressRuleTcpV1Beta1
    {
        [DataMember(Name = "backend", EmitDefaultValue = false)]
        public V1beta1IngressBackend Backend { get; set; }

        [DataMember(Name = "port", EmitDefaultValue = false)]
        public string Port { get; set; }
    }
}
