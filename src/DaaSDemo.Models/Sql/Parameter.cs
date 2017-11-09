using Newtonsoft.Json;
using System;
using System.Data;
using System.Data.SqlClient;

namespace DaaSDemo.Models.Sql
{
    /// <summary>
    ///     A T-SQL parameter for a command or query.
    /// </summary>
    public class Parameter
    {
        /// <summary>
        ///     The parameter name.
        /// </summary>
        [JsonProperty]
        public string Name { get; set; }

        /// <summary>
        ///     The parameter data-type.
        /// </summary>
        [JsonProperty]
        public SqlDbType DataType { get; set; }

        /// <summary>
        ///     The parameter size.
        /// </summary>
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public int Size { get; set; }

        /// <summary>
        ///     Create a <see cref="SqlParameter"/> from the <see cref="Parameter"/>.
        /// </summary>
        /// <returns>
        ///     The <see cref="SqlParameter"/>.
        /// </returns>
        public SqlParameter ToSqlParameter() => new SqlParameter(Name, DataType, Size);

        /// <summary>
        ///     Create a <see cref="Parameter"/> from the specified <see cref="SqlParameter"/>.
        /// </summary>
        /// <param name="sqlParameter">
        ///     The <see cref="SqlParameter"/>.
        /// </param>
        /// <returns>
        ///     The <see cref="Parameter"/>.
        /// </returns>
        public static Parameter FromSqlParameter(SqlParameter sqlParameter)
        {
            if (sqlParameter == null)
                throw new ArgumentNullException(nameof(sqlParameter));
            
            return new Parameter
            {
                Name = sqlParameter.ParameterName,
                DataType = sqlParameter.SqlDbType,
                Size = sqlParameter.Size
            };
        }
    }
}
