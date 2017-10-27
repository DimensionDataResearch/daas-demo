namespace DaaSDemo.Provisioning.Messages
{
    using Data.Models;

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
        public DatabaseProvisioning(int databaseId)
            : base(databaseId, ProvisioningStatus.Ready)
        {
        }
    }
}