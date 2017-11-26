namespace DaaSDemo.Models.DatabaseProxy
{
    /// <summary>
    ///     Represents a T-SQL error.
    /// </summary>
    public class SqlError
    {
        /// <summary>
        ///     The kind of error.
        /// </summary>
        public SqlErrorKind Kind { get; set; }

        /// <summary>
        ///     The error message.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        ///     The T-SQL error class (if <see cref="SqlErrorKind.TSql"/>).
        /// </summary>
        public byte Class { get; set; }

        /// <summary>
        ///     The T-SQL error number (if <see cref="SqlErrorKind.TSql"/>).
        /// </summary>
        public int Number { get; set; }

        /// <summary>
        ///     The name of the procedure where the T-SQL error occurred (if <see cref="SqlErrorKind.TSql"/>).
        /// </summary>
        public string Procedure { get; set; }

        /// <summary>
        ///     The T-SQL error source (if <see cref="SqlErrorKind.TSql"/>).
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        ///     The T-SQL error state (if <see cref="SqlErrorKind.TSql"/>).
        /// </summary>
        public byte State { get; set; }

        /// <summary>
        ///     The line number where the T-SQL error occurred (if <see cref="SqlErrorKind.TSql"/>).
        /// </summary>
        public int LineNumber { get; set; }
    }
}
