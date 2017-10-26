namespace DaaSDemo.Provisioning.Messages
{
    using Data.Models;

    /// <summary>
    ///     Message indicating that a database server's provisioning status has changed.
    /// </summary>
    public abstract class ServerStatusChanged
    {
        /// <summary>
        ///     Create a new <see cref="ServerStatusChanged"/>.
        /// </summary>
        /// <param name="serverId">
        ///     The Id of the server whose provisioning status has changed.
        /// </param>
        /// <param name="status">
        ///     The server's current provisioning status.
        /// </param>
        protected ServerStatusChanged(int serverId, ProvisioningStatus status)
        {
            ServerId = serverId;
            Status = status;
        }

        /// <summary>
        ///     The Id of the server whose provisioning status has changed.
        /// </summary>
        public int ServerId { get; }

        /// <summary>
        ///     The server's current provisioning status.
        /// </summary>
        public ProvisioningStatus Status { get; }
    }
}