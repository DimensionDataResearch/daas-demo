namespace DaaSDemo.Provisioning.Messages
{
    using Data.Models;
    
    /// <summary>
    ///     Message indicating that an error was encountered while provisioning a database server.
    /// </summary>
    public class ServerReconfigurationFailed
        : ServerStatusChanged
    {
        /// <summary>
        ///     Create a new <see cref="ServerReconfigurationFailed"/> message.
        /// </summary>
        /// <param name="serverId">
        ///     The Id of the server for which reconfiguration was unsuccessful.
        /// </param>
        public ServerReconfigurationFailed(int serverId)
            : base(serverId, ProvisioningStatus.Error)
        {
        }
    }
}
