namespace DaaSDemo.Models.Data
{
    /// <summary>
    ///     Represents the storage configuration for a database.
    /// </summary>
    public class DatabaseInstanceStorage
    {
        /// <summary>
        ///     The amount of storage (in MB) allocated to the database (an additional 20% of storage will be reserved for transaction logs).
        /// </summary>
        public int SizeMB { get; set; }
    }
}
