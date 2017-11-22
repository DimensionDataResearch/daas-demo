namespace DaaSDemo.Provisioning.Messages
{
    using Models.Data;

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
        /// <param name="phase">
        ///     The server's current provisioning phase (if any).
        /// </param>
        protected ServerStatusChanged(string serverId, ProvisioningStatus status)
        {
            ServerId = serverId;
            Status = status;
        }

        /// <summary>
        ///     Create a new <see cref="ServerStatusChanged"/>.
        /// </summary>
        /// <param name="serverId">
        ///     The Id of the server whose provisioning status has changed.
        /// </param>
        /// <param name="status">
        ///     The server's current provisioning status.
        /// </param>
        /// <param name="phase">
        ///     The server's current provisioning phase (if any).
        /// </param>
        protected ServerStatusChanged(string serverId, ServerProvisioningPhase phase)
        {
            ServerId = serverId;
            Phase = phase;
        }

        /// <summary>
        ///     Create a new <see cref="ServerStatusChanged"/>.
        /// </summary>
        /// <param name="serverId">
        ///     The Id of the server whose provisioning status has changed.
        /// </param>
        /// <param name="status">
        ///     The server's current provisioning status.
        /// </param>
        /// <param name="phase">
        ///     The server's current provisioning phase (if any).
        /// </param>
        protected ServerStatusChanged(string serverId, ProvisioningStatus status, ServerProvisioningPhase phase)
        {
            ServerId = serverId;
            Status = status;
            Phase = phase;
        }

        /// <summary>
        ///     The Id of the server whose provisioning status has changed.
        /// </summary>
        public string ServerId { get; }

        /// <summary>
        ///     The server's current provisioning status.
        /// </summary>
        public ProvisioningStatus? Status { get; }

        /// <summary>
        ///     The server's current provisioning phase.
        /// </summary>
        public ServerProvisioningPhase? Phase { get; }
    }
}
