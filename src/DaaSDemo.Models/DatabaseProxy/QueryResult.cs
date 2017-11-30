using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DaaSDemo.Models.DatabaseProxy
{
    /// <summary>
    ///     Response body when executing a T-SQL query.
    /// </summary>
    public class QueryResult
        : SqlResult
    {
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
