using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DaaSDemo.Models.Sql
{
    /// <summary>
    ///     Response body when executing a T-SQL query.
    /// </summary>
    public class QueryResult
    {
        /// <summary>
        ///     Was the T-SQL successfully executed?
        /// </summary>
        public bool Success => Errors.Count == 0;

        /// <summary>
        ///     The query's result code (usually the number of rows affected).
        /// </summary>
        public int ResultCode { get; set; }

        /// <summary>
        ///     The T-SQL to execute.
        /// </summary>
        [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Reuse)]
        public List<string> Messages { get; } = new List<string>();

        /// <summary>
        ///     Errors (if any) encountered while executing the T-SQL.
        /// </summary>
        [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Reuse)]
        public List<SqlError> Errors { get; } = new List<SqlError>();

        /// <summary>
        ///     The result set(s) returned by the query.
        /// </summary>
        public List<ResultSet> ResultSets { get; } = new List<ResultSet>();
    }

    public class ResultSet
    {
        [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Reuse)]
        public List<ResultRow> Rows { get; } = new List<ResultRow>();
    }

    public class ResultRow
    {
        [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Reuse)]
        public Dictionary<string, JValue> Columns { get; } = new Dictionary<string, JValue>();
    }
}
