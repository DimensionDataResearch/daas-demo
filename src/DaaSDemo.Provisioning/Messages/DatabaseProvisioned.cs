namespace DaaSDemo.Provisioning.Messages
{
    using Models.Data;

    /// <summary>
    ///     Message indicating that a database has been provisioned.
    /// </summary>
    public class DatabaseProvisioned
            : DatabaseStatusChanged
    {
        /// <summary>
        ///     Create a new <see cref="DatabaseProvisioned"/> message.
        /// </summary>
        /// <param name="databaseId">
        ///     The Id of the database that was provisioned.
        /// </param>
        public DatabaseProvisioned(int databaseId)
            : base(databaseId, ProvisioningStatus.Ready)
        {
        }
    }
}
