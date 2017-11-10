namespace DaaSDemo.Provisioning.Messages
{
    using Models.Data;

    /// <summary>
    ///     Message indicating that a database's provisioning status has changed.
    /// </summary>
    public abstract class DatabaseStatusChanged
    {
        /// <summary>
        ///     Create a new <see cref="DatabaseStatusChanged"/>.
        /// </summary>
        /// <param name="databaseId">
        ///     The Id of the database whose provisioning status has changed.
        /// </param>
        /// <param name="status">
        ///     The database's current provisioning status.
        /// </param>
        protected DatabaseStatusChanged(int databaseId, ProvisioningStatus status)
        {
            DatabaseId = databaseId;
            Status = status;
        }

        /// <summary>
        ///     The Id of the database whose provisioning status has changed.
        /// </summary>
        public int DatabaseId { get; }

        /// <summary>
        ///     The database's current provisioning status.
        /// </summary>
        public ProvisioningStatus Status { get; }
    }
}