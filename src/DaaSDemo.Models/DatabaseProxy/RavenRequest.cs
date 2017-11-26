using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace DaaSDemo.Models.DatabaseProxy
{
    /// <summary>
    ///     The base class for RavenDB requests.
    /// </summary>
    public abstract class RavenRequest
    {
        /// <summary>
        ///     The Id of the tenant server on which the request is to be executed.
        /// </summary>
        [JsonProperty]
        [Required(AllowEmptyStrings = false)]
        public string ServerId { get; set; }

        /// <summary>
        ///     The Id of the database in which the request is to be executed, or 0 for the master database.
        /// </summary>
        [JsonProperty]
        [Required(AllowEmptyStrings = false)]
        public string DatabaseId { get; set; }
    }
}
