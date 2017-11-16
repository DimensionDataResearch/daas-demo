using KubeNET.Swagger.Model;
using System.Runtime.Serialization;

namespace DaaSDemo.KubeClient.Models
{
    [DataContract]
    public class VoyagerIngressV1Beta1
    {
        [DataMember(Name = "apiVersion", EmitDefaultValue = false)]
        public string ApiVersion { get; set; }

        [DataMember(Name = "kind", EmitDefaultValue = false)]
        public string Kind { get; set; }

        [DataMember(Name = "metadata", EmitDefaultValue = false)]
        public V1ObjectMeta Metadata { get; set; }

        [DataMember(Name = "spec", EmitDefaultValue = false)]
        public VoyagerIngressSpecV1Beta1 Spec { get; set; }

        public V1beta1IngressStatus Status { get; set; }
    }
}
