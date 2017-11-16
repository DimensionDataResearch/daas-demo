using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     UserInfo holds the information about the user needed to implement the user.Info interface.
    /// </summary>
    public class UserInfoV1Beta1
    {
        /// <summary>
        ///     Any additional information provided by the authenticator.
        /// </summary>
        [JsonProperty("extra")]
        public Dictionary<string, string> Extra { get; set; }

        /// <summary>
        ///     The names of groups this user is a part of.
        /// </summary>
        [JsonProperty("groups")]
        public List<string> Groups { get; set; }

        /// <summary>
        ///     A unique value that identifies this user across time. If this user is deleted and another user by the same name is added, they will have different UIDs.
        /// </summary>
        [JsonProperty("uid")]
        public string Uid { get; set; }
    }
}
