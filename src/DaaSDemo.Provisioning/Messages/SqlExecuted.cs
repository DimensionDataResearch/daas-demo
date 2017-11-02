using System;

namespace DaaSDemo.Provisioning.Messages
{
    /// <summary>
    ///     Message indicating that T-SQL was successfully executed.
    /// </summary>
    public class SqlExecuted
    {
        /// <summary>
        ///     Create a new <see cref="SqlExecuted"/> message.
        /// </summary>
        /// <param name="jobName">
        ///     The name of the Job that executed the T-SQL.
        /// </param>
        /// <param name="serverId">
        ///     The Id of the SQL server instance where the T-SQL was executed.
        /// </param>
        /// <param name="databaseName">
        ///     The name of the target database where the T-SQL will be executed.
        /// </param>
        /// <param name="result">
        ///     A <see cref="SqlExecutionResult"/> representing the result of the execution.
        /// </param>
        /// <param name="output">
        ///     The resulting output from executing the T-SQL.
        /// </param>
        public SqlExecuted(string jobName, int serverId, string databaseName, SqlExecutionResult result, string output)
        {
            if (String.IsNullOrWhiteSpace(jobName))
                throw new ArgumentException("Argument cannot be null, empty, or entirely composed of whitespace: 'jobName'.", nameof(jobName));
            
            if (String.IsNullOrWhiteSpace(databaseName))
                throw new ArgumentException("Argument cannot be null, empty, or entirely composed of whitespace: 'databaseName'.", nameof(databaseName));

            if (output == null)
                throw new ArgumentNullException(nameof(output));

            JobName = jobName;
            DatabaseName = databaseName;
            Result = result;
            Output = output;
        }

        /// <summary>
        ///     The name of the Job that executed the T-SQL.
        /// </summary>
        public string JobName { get; }

        /// <summary>
        ///     The Id of the SQL server instance where the T-SQL was executed.
        /// </summary>
        public int ServerId { get; }

        /// <summary>
        ///     The name of the target database where the T-SQL was executed.
        /// </summary>
        public string DatabaseName { get; }

        /// <summary>
        ///     A <see cref="SqlExecutionResult"/> representing the result of the execution.
        /// </summary>
        public SqlExecutionResult Result { get; }

        /// <summary>
        ///     Was the T-SQL executed successfully?
        /// </summary>
        public bool Success => Result == SqlExecutionResult.Success;

        /// <summary>
        ///     The resulting output from executing the T-SQL.
        /// </summary>
        public string Output { get; }
    }
}
