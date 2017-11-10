namespace DaaSDemo.Provisioning.Messages
{
    using Models.Data;

    /// <summary>
    ///     Message indicating that a database has been de-provisioned.
    /// </summary>
    public class DatabaseDeprovisioned
            : DatabaseStatusChanged
    {
        /// <summary>
        ///     Create a new <see cref="DatabaseDeprovisioned"/> message.
        /// </summary>
        /// <param name="databaseId">
        ///     The Id of the database that was de-provisioned.
        /// </param>
        public DatabaseDeprovisioned(int databaseId)
            : base(databaseId, ProvisioningStatus.Deprovisioned)
        {
        }
    }
}
