using System.Runtime.Serialization;

namespace DaaSDemo.KubeClient.Models
{
    [DataContract]
    public class V1Beta1VoyagerIngressRule
    {
        [DataMember(Name = "host", EmitDefaultValue = false)]
        public string Host { get; set; }

        [DataMember(Name = "tcp", EmitDefaultValue = false)]
        public V1Beta1VoyagerIngressRuleTcp Tcp { get; set; }
    }
}