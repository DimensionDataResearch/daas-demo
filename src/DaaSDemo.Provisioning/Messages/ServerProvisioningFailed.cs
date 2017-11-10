namespace DaaSDemo.Provisioning.Messages
{
    using Models.Data;

    /// <summary>
    ///     Message indicating that an error was encountered while provisioning a database server.
    /// </summary>
    public class ServerProvisioningFailed
        : ServerStatusChanged
    {
        /// <summary>
        ///     Create a new <see cref="ServerProvisioned"/> message.
        /// </summary>
        /// <param name="serverId">
        ///     The Id of the server for which provisioning was unsuccessful.
        /// </param>
        public ServerProvisioningFailed(int serverId)
            : base(serverId, ProvisioningStatus.Error)
        {
        }
    }
}
