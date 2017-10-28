namespace DaaSDemo.Provisioning.Messages
{
    using Data.Models;

    /// <summary>
    ///     Message indicating that an error was encountered while de-provisioning a database.
    /// </summary>
    public class DatabaseDeprovisioningFailed
        : DatabaseStatusChanged
    {
        /// <summary>
        ///     Create a new <see cref="DatabaseDeprovisioningFailed"/> message.
        /// </summary>
        /// <param name="databaseId">
        ///     The Id of the database for which de-provisioning was unsuccessful.
        /// </param>
        public DatabaseDeprovisioningFailed(int databaseId)
            : base(databaseId, ProvisioningStatus.Error)
        {
        }
    }
}
