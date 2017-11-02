namespace DaaSDemo.Provisioning.Messages
{
    /// <summary>
    ///     Represents the result of executing T-SQL.
    /// </summary>
    public enum SqlExecutionResult
    {
        /// <summary>
        ///     Unexpected error.
        /// </summary>
        Failed = 0,

        /// <summary>
        ///     T-SQL was successfully executed.
        /// </summary>
        Success = 1,

        /// <summary>
        ///     T-SQL failed due to a Job timeout.
        /// </summary>
        JobTimeout = 2,

        /// <summary>
        ///     T-SQL Job was deleted.
        /// </summary>
        JobDeleted = 3
    }
}
