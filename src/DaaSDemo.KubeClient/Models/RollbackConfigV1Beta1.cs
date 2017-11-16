using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     No description provided.
    /// </summary>
    public class RollbackConfigV1Beta1
    {
        /// <summary>
        ///     The revision to rollback to. If set to 0, rollback to the last revision.
        /// </summary>
        [JsonProperty("revision")]
        public int Revision { get; set; }
    }
}
