using KubeNET.Swagger.Model;
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
}
