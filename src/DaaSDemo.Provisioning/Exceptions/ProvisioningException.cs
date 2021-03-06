using System;
using System.Collections.Generic;
using System.Text;

namespace DaaSDemo.Provisioning.Exceptions
{
    using Models.DatabaseProxy;

    /// <summary>
    ///     Exception raised when an error is encountered while provisioning.
    /// </summary>
    public class ProvisioningException
        : Exception
    {
        /// <summary>
        ///     Create a new <see cref="ProvisioningException"/>.
        /// </summary>
        /// <param name="message">
        ///     The exception message.
        /// </param>
        public ProvisioningException(string message)
            : base(message)
        {
        }

        /// <summary>
        ///     Create a new <see cref="ProvisioningException"/>.
        /// </summary>
        /// <param name="message">
        ///     The exception message.
        /// </param>
        /// <param name="innerException">
        ///     The exception that caused this exception to be raised.
        /// </param>
        public ProvisioningException(string message, System.Exception innerException)
            : base(message, innerException)
        {
        }
    }

    /// <summary>
    ///     Exception raised when a fatal error is encountered while provisioning.
    /// </summary>
    public class FatalProvisioningException
        : ProvisioningException
    {
        /// <summary>
        ///     Create a new <see cref="FatalProvisioningException"/>.
        /// </summary>
        /// <param name="message">
        ///     The exception message.
        /// </param>
        public FatalProvisioningException(string message)
            : base(message)
        {
        }

        /// <summary>
        ///     Create a new <see cref="FatalProvisioningException"/>.
        /// </summary>
        /// <param name="message">
        ///     The exception message.
        /// </param>
        /// <param name="innerException">
        ///     The exception that caused this exception to be raised.
        /// </param>
        public FatalProvisioningException(string message, System.Exception innerException)
            : base(message, innerException)
        {
        }
    }

    /// <summary>
    ///     Exception raised when one or more errors are encountered while executing SQL via the Database Proxy API.
    /// </summary>
    public class SqlExecutionException
        : ProvisioningException
    {
        /// <summary>
        ///     Create a new <see cref="SqlExecutionException"/>.
        /// </summary>
        /// <param name="message">
        ///     The exception message.
        /// </param>
        /// <param name="serverId">
        ///     The Id of the database server where the error occurred.
        /// </param>
        /// <param name="databaseId">
        ///     The Id of the database where the error occurred.
        /// </param>
        /// <param name="sqlMessages">
        ///     Messages (if any) generated during T-SQL proxy.
        /// </param>
        /// <param name="sqlErrors">
        ///     An <see cref="SqlError"/> list representing the error(s) that occurred.
        /// </param>
        public SqlExecutionException(string message, string serverId, string databaseId, IEnumerable<string> sqlMessages, IEnumerable<SqlError> sqlErrors)
            : base(message)
        {
            if (sqlMessages == null)
                throw new ArgumentNullException(nameof(sqlMessages));

            if (sqlErrors == null)
                throw new ArgumentNullException(nameof(sqlErrors));
            
            ServerId = serverId;
            DatabaseId = databaseId;
            SqlMessages = new List<string>(sqlMessages);
            SqlErrors = new List<SqlError>(sqlErrors);
        }

        /// <summary>
        ///     Create a new <see cref="SqlExecutionException"/>.
        /// </summary>
        /// <param name="message">
        ///     The exception message.
        /// </param>
        /// <param name="innerException">
        ///     The exception that caused this exception to be raised.
        /// </param>
        /// <param name="serverId">
        ///     The Id of the database server where the error occurred.
        /// </param>
        /// <param name="databaseId">
        ///     The Id of the database where the error occurred.
        /// </param>
        /// <param name="sqlMessages">
        ///     Messages (if any) generated during T-SQL proxy.
        /// </param>
        /// <param name="sqlErrors">
        ///     An <see cref="SqlError"/> list representing the error(s) that occurred.
        /// </param>
        public SqlExecutionException(string message, Exception innerException, string serverId, string databaseId, IEnumerable<string> sqlMessages, IEnumerable<SqlError> sqlErrors)
            : base(message, innerException)
        {
            if (sqlMessages == null)
                throw new ArgumentNullException(nameof(sqlMessages));

            if (sqlErrors == null)
                throw new ArgumentNullException(nameof(sqlErrors));
            
            ServerId = serverId;
            DatabaseId = databaseId;
            SqlMessages = new List<string>(sqlMessages);
            SqlErrors = new List<SqlError>(sqlErrors);
        }

        /// <summary>
        ///     The Id of the database server where the error occurred.
        /// </summary>
        public string ServerId { get; }

        /// <summary>
        ///     The Id of the database where the error occurred.
        /// </summary>
        public string DatabaseId { get; }

        /// <summary>
        ///     Messages (if any) generated by the command or query that failed.
        /// </summary>
        public IReadOnlyList<string> SqlMessages { get; }

        /// <summary>
        ///     An <see cref="SqlError"/> list representing the error(s) that occurred.
        /// </summary>
        public IReadOnlyList<SqlError> SqlErrors { get; }

        /// <summary>
        ///     Get a string representation of the exception.
        /// </summary>
        /// <returns>
        ///     A string containing the exception details.
        /// </returns>
        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder(
                base.ToString()
            );
            
            stringBuilder.AppendLine();

            foreach (SqlError sqlError in SqlErrors)
            {
                if (sqlError.Kind == SqlErrorKind.TSql)
                {
                    stringBuilder.AppendLine(
                        $"[{sqlError.Kind}] Error on line {sqlError.LineNumber} ({sqlError.Number}): {sqlError.Message}"
                    );
                }
                else
                {
                    stringBuilder.AppendLine(
                        $"[{sqlError.Kind}] {sqlError.Message}"
                    );
                }
            }

            return stringBuilder.ToString();
        }
    }
}
