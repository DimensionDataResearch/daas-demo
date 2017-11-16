using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     StorageClass describes the parameters for a class of storage for which PersistentVolumes can be dynamically provisioned.
    ///     
    ///     StorageClasses are non-namespaced; the name of the storage class according to etcd is in ObjectMeta.Name.
    /// </summary>
    public class StorageClassV1Beta1 : KubeResourceV1
    {
        /// <summary>
        ///     Provisioner indicates the type of the provisioner.
        /// </summary>
        [JsonProperty("provisioner")]
        public string Provisioner { get; set; }

        /// <summary>
        ///     Parameters holds the parameters for the provisioner that should create volumes of this storage class.
        /// </summary>
        [JsonProperty("parameters", NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, string> Parameters { get; set; } = new Dictionary<string, string>();
    }
}
