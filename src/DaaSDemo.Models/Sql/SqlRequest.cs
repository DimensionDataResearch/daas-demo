using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace DaaSDemo.Models.Sql
{
    /// <summary>
    ///     The base class for T-SQL requests.
    /// /// </summary>
    public abstract class SqlRequest
    {
        /// <summary>
        ///     The Id of the tenant server on which the request is to be executed.
        /// </summary>
        [JsonProperty]
        [Range(1, Int32.MaxValue)]
        public int ServerId { get; set; }

        /// <summary>
        ///     The Id of the database in which the request is to be executed, or 0 for the master database.
        /// </summary>
        [JsonProperty]
        [Range(0, Int32.MaxValue)]
        public int DatabaseId { get; set; }

        /// <summary>
        ///     T-SQL parameters (if any) for the request.
        /// </summary>
        [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Reuse)]
        public List<Parameter> Parameters = new List<Parameter>();

        /// <summary>
        ///     The list of T-SQL statements to execute.
        /// </summary>
        [Required]
        [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Reuse)]
        public List<string> Sql { get; set; } = new List<string>();

        /// <summary>
        ///     Execute the request as the server's admin user ("sa")?
        /// </summary>
        [JsonProperty]
        public bool ExecuteAsAdminUser { get; set; }
    }
}
