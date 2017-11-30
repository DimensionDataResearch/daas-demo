namespace DaaSDemo.Models.Data
{
    /// <summary>
    ///     Represents the storage configuration for a database server.
    /// </summary>
    public class DatabaseServerStorage
        : IDeepCloneable<DatabaseServerStorage>
    {
        /// <summary>
        ///     The total amount of storage (in MB) allocated to the server.
        /// </summary>
        public int SizeMB { get; set; }

        /// <summary>
        ///     Create a deep clone of the <see cref="DatabaseServerStorage"/>.
        /// </summary>
        /// <returns>
        ///     The cloned <see cref="DatabaseServerStorage"/>.
        /// </returns>
        public virtual DatabaseServerStorage Clone()
        {
            return new DatabaseServerStorage
            {
                SizeMB = SizeMB
            };
        }
    }
}
