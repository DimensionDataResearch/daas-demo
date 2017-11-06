namespace DaaSDemo.Models.Sql
{
    /// <summary>
    ///     Request body when executing a T-SQL command (i.e. a non-query).
    /// </summary>
    public class Command
    {
        /// <summary>
        ///     The Id of the tenant server on which the command is to be executed.
        /// </summary>
        public int ServerId { get; set; }

        /// <summary>
        ///     The Id of the database in which the command is to be executed, or 0 for the master database.
        /// </summary>
        public int DatabaseId { get; set; }

        /// <summary>
        ///     The T-SQL to execute.
        /// </summary>
        public string Sql { get; set; }

        // TODO: Add support for parameters.

        /// <summary>
        ///     Execute the command as the server's admin user ("sa")?
        /// </summary>
        public bool ExecuteAsAdminUser { get; set; }
    }
}
