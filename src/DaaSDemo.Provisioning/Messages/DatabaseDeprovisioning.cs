namespace DaaSDemo.Provisioning.Messages
{
    using Data.Models;

    /// <summary>
    ///     Message indicating that a database is being de-provisioned.
    /// </summary>
    public class DatabaseDeprovisioning
            : DatabaseStatusChanged
    {
        /// <summary>
        ///     Create a new <see cref="DatabaseDeprovisioned"/> message.
        /// </summary>
        /// <param name="databaseId">
        ///     The Id of the database that is being de-provisioned.
        /// </param>
        public DatabaseDeprovisioning(int databaseId)
            : base(databaseId, ProvisioningStatus.Deprovisioning)
        {
        }
    }
}