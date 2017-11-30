namespace DaaSDemo.Models.Data
{
    /// <summary>
    ///     Well-known settings for a <see cref="DatabaseServer"/> representing an SQL Server instance.
    /// </summary>
    public class SqlServerSettings
        : DatabaseServerSettings
    {
        /// <summary>
        ///     The server's administrative ("sa" user) password (if required by the server kind).
        /// </summary>
        public string AdminPassword { get; set; }

        /// <summary>
        ///     Create a deep clone of the <see cref="DatabaseServerSettings"/>.
        /// </summary>
        /// <returns>
        ///     The cloned <see cref="DatabaseServerSettings"/>.
        /// </returns>
        public override DatabaseServerSettings Clone()
        {
            return new SqlServerSettings
            {
                AdminPassword = AdminPassword,
                Storage = Storage.Clone()
            };
        }
    }
}
