namespace DaaSDemo.Provisioning.Messages
{
    using Data.Models;

    /// <summary>
    ///     Message indicating that an error was encountered while de-provisioning a database server.
    /// </summary>
    public class ServerDeprovisioningFailed
        : ServerStatusChanged
    {
        /// <summary>
        ///     Create a new <see cref="ServerDeprovisioned"/> message.
        /// </summary>
        /// <param name="serverId">
        ///     The Id of the server for which de-provisioning was unsuccessful.
        /// </param>
        public ServerDeprovisioningFailed(int serverId)
            : base(serverId, ProvisioningStatus.Error)
        {
        }
    }
}