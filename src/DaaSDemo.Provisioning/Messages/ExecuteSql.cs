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
        /// <param name="databaseName">
        ///     The name of the target database.
        /// </param>
        /// <param name="sql">
        ///     The T-SQL to execute.
        /// </param>
        /// <param name="jobNameSuffix">
        ///     A unique suffix for the name of the Job that executes the T-SQL.
        /// </param>
        public ExecuteSql(string databaseName, string sql, string jobNameSuffix)
        {
            if (String.IsNullOrWhiteSpace(databaseName))
                throw new ArgumentException("Argument cannot be null, empty, or entirely composed of whitespace: 'databaseName'.", nameof(databaseName));

            if (String.IsNullOrWhiteSpace(jobNameSuffix))
                throw new ArgumentException("Argument cannot be null, empty, or entirely composed of whitespace: 'jobNameSuffix'.", nameof(jobNameSuffix));
            
            if (String.IsNullOrWhiteSpace(sql))
                throw new ArgumentException("Argument cannot be null, empty, or entirely composed of whitespace: 'sql'.", nameof(sql));

            DatabaseName = databaseName;
            Sql = sql;
            JobNameSuffix = jobNameSuffix;
        }

        /// <summary>
        ///     The name of the target database.
        /// </summary>
        public string DatabaseName { get; }

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
