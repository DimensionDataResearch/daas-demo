namespace DaaSDemo.Provisioning.Messages
{
    using Models.Data;

    /// <summary>
    ///     Message indicating that a database is being provisioned.
    /// </summary>
    public class DatabaseProvisioning
            : DatabaseStatusChanged
    {
        /// <summary>
        ///     Create a new <see cref="DatabaseProvisioned"/> message.
        /// </summary>
        /// <param name="databaseId">
        ///     The Id of the database that is being provisioned.
        /// </param>
        public DatabaseProvisioning(string databaseId)
            : base(databaseId, ProvisioningStatus.Ready)
        {
        }
    }
}