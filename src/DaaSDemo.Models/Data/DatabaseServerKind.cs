namespace DaaSDemo.Models.Data
{
    /// <summary>
    ///     Well-known kinds of database server.
    /// </summary>
    public enum DatabaseServerKind
    {
        /// <summary>
        ///     An unknown server kind.
        /// </summary>
        Unknown = 0,

        /// <summary>
        ///     Microsoft SQL Server.
        /// </summary>
        SqlServer = 1,

        /// <summary>
        ///     RavenDB.
        /// </summary>
        RavenDB = 2
    }
}
