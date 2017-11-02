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
        /// <param name="jobName">
        ///     A unique name for the Job that executes the T-SQL; if a job with this name already exists, it will be deleted (once completed) and a new Job created.
        /// </param>
        /// <param name="databaseName">
        ///     The name of the target database where the T-SQL will be executed.
        /// </param>
        /// <param name="sql">
        ///     The T-SQL to execute.
        /// </param>
        public ExecuteSql(string jobName, string databaseName, string sql)
        {
            if (String.IsNullOrWhiteSpace(sql))
                throw new ArgumentException("Argument cannot be null, empty, or entirely composed of whitespace: 'sql'.", nameof(sql));

            DatabaseName = databaseName;
            Sql = sql;
        }

        /// <summary>
        ///     A unique name for the Job that executes the T-SQL.
        /// </summary>
        /// <remarks>
        ///     If a job with this name already exists, it will be deleted (once completed) and a new Job created.
        /// </remarks>
        public string JobName { get; }

        /// <summary>
        ///     The name of the target database where the T-SQL will be executed.
        /// </summary>
        public string DatabaseName { get; }

        /// <summary>
        ///     The T-SQL to execute.
        /// </summary>
        public string Sql { get; }
    }
}
