namespace DaaSDemo.Provisioning.Messages
{
    using Models.Data;

    /// <summary>
    ///     Message indicating that an error was encountered while provisioning a database.
    /// </summary>
    public class DatabaseProvisioningFailed
        : DatabaseStatusChanged
    {
        /// <summary>
        ///     Create a new <see cref="DatabaseProvisioned"/> message.
        /// </summary>
        /// <param name="databaseId">
        ///     The Id of the database for which provisioning was unsuccessful.
        /// </param>
        public DatabaseProvisioningFailed(int databaseId)
            : base(databaseId, ProvisioningStatus.Error)
        {
        }
    }
}
