using System;

namespace DaaSDemo.Provisioning.Messages
{
    /// <summary>
    ///     Message requesting execution of T-SQL.
    /// </summary>
    public class ExecuteSql
    {
        /// <summary>
        ///     Create a new <see cref="ExecuteSql"/> message.
        /// </summary>
        /// <param name="jobNameSuffix">
        ///     A unique suffix for the name of the Job that executes the T-SQL.
        /// </param>
        /// <param name="sql">
        ///     The T-SQL to execute.
        /// </param>
        public ExecuteSql(string jobNameSuffix, string sql)
        {
            if (String.IsNullOrWhiteSpace(jobNameSuffix))
                throw new ArgumentException("Argument cannot be null, empty, or entirely composed of whitespace: 'jobNameSuffix'.", nameof(jobNameSuffix));
            
            if (String.IsNullOrWhiteSpace(sql))
                throw new ArgumentException("Argument cannot be null, empty, or entirely composed of whitespace: 'sql'.", nameof(sql));

            JobNameSuffix = jobNameSuffix;
            Sql = sql;
        }

        /// <summary>
        ///     A unique suffix for the name of the Job that executes the T-SQL.
        /// </summary>
        /// <remarks>
        ///     If a job with this name already exists, it will be deleted (once completed) and a new Job created.
        /// </remarks>
        public string JobNameSuffix { get; }

        /// <summary>
        ///     The T-SQL to execute.
        /// </summary>
        public string Sql { get; }
    }
}
