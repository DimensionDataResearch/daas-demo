using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DaaSDemo.Models.Sql
{
    /// <summary>
    ///     Request body when executing a T-SQL command (i.e. a non-query).
    /// </summary>
    public class Command
    {
        /// <summary>
        ///     The Id of the tenant server on which the command is to be executed.
        /// </summary>
        [JsonProperty]
        [Range(1, Int32.MaxValue)]
        public int ServerId { get; set; }

        /// <summary>
        ///     The Id of the database in which the command is to be executed, or 0 for the master database.
        /// </summary>
        [JsonProperty]
        [Range(0, Int32.MaxValue)]
        public int DatabaseId { get; set; }

        /// <summary>
        ///     T-SQL parameters (if any) for the command.
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
        ///     Execute the command as the server's admin user ("sa")?
        /// </summary>
        [JsonProperty]
        public bool ExecuteAsAdminUser { get; set; }
    }
}
