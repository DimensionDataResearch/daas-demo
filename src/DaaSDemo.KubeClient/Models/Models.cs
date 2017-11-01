using KubeNET.Swagger.Model;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     A <see cref="V1VolumeMount"/> with the "subPath" property.
    /// </summary>
    [DataContract]
    public class V1VolumeMountWithSubPath
        : V1VolumeMount
    {
        /// <summary>
        ///     The volume sub-path (if any).
        /// </summary>
        [DataMember(Name = "subPath", EmitDefaultValue = false)]
        public string SubPath { get; set; }
    }

    [DataContract]
    public class V1Beta1VoyagerIngress
    {
        [DataMember(Name = "apiVersion", EmitDefaultValue = false)]
        public string ApiVersion { get; set; }

        [DataMember(Name = "kind", EmitDefaultValue = false)]
        public string Kind { get; set; }

        [DataMember(Name = "metadata", EmitDefaultValue = false)]
        public V1ObjectMeta Metadata { get; set; }

        [DataMember(Name = "spec", EmitDefaultValue = false)]
        public V1BetaVoyagerIngressSpec Spec { get; set; }

        public V1beta1IngressStatus Status { get; set; }
    }

    [DataContract]
    public class V1BetaVoyagerIngressSpec
    {
        [DataMember(Name = "tls", EmitDefaultValue = false)]
        public List<V1beta1IngressTLS> Tls { get; set; }

        [DataMember(Name = "rules", EmitDefaultValue = false)]
        public List<V1Beta1VoyagerIngressRule> Rules { get; set; }
    }

    [DataContract]
    public class V1Beta1VoyagerIngressRule
    {
        [DataMember(Name = "host", EmitDefaultValue = false)]
        public string Host { get; set; }

        [DataMember(Name = "tcp", EmitDefaultValue = false)]
        public V1Beta1VoyagerIngressRuleTcp Tcp { get; set; }
    }

    [DataContract]
    public class V1Beta1VoyagerIngressRuleTcp
    {
        [DataMember(Name = "backend", EmitDefaultValue = false)]
        public V1beta1IngressBackend Backend { get; set; }

        [DataMember(Name = "port", EmitDefaultValue = false)]
        public string Port { get; set; }
    }
}