namespace DaaSDemo.Models.Sql
{
    /// <summary>
    ///     Represents a kind of error encountered while executing T-SQL.
    /// </summary>
    public enum SqlErrorKind
    {
        /// <summary>
        ///     A T-SQL error.
        /// </summary>
        TSql = 1,

        /// <summary>
        ///     An infrastructure error (e.g. connection timeout).
        /// </summary>
        Infrastructure = 2
    }
}
