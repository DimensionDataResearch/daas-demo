using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     ControllerRevision implements an immutable snapshot of state data. Clients are responsible for serializing and deserializing the objects that contain their internal state. Once a ControllerRevision has been successfully created, it can not be updated. The API Server will fail validation of all requests that attempt to mutate the Data field. ControllerRevisions may, however, be deleted. Note that, due to its use by both the DaemonSet and StatefulSet controllers for update and rollback, this object is beta. However, it may be subject to name and representation changes in future releases, and clients should not depend on its stability. It is primarily for internal use by controllers.
    /// </summary>
    public class ControllerRevisionV1Beta1 : KubeResource
    {
        /// <summary>
        ///     Data is the serialized representation of the state.
        /// </summary>
        [JsonProperty("data")]
        public RawExtensionRuntime Data { get; set; }

        /// <summary>
        ///     Standard object's metadata. More info: https://git.k8s.io/community/contributors/devel/api-conventions.md#metadata
        /// </summary>
        [JsonProperty("metadata")]
        public ObjectMetaV1 Metadata { get; set; }

        /// <summary>
        ///     Revision indicates the revision of the state represented by Data.
        /// </summary>
        [JsonProperty("revision")]
        public int Revision { get; set; }
    }
}
