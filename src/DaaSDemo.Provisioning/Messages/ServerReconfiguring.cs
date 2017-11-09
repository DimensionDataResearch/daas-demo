namespace DaaSDemo.Provisioning.Messages
{
    using Data.Models;

    /// <summary>
    ///     Message indicating that a database server is being reconfigured.
    /// </summary>
    public class ServerReconfiguring
        : ServerStatusChanged
    {
        /// <summary>
        ///     Create a new <see cref="ServerReconfiguring"/> message.
        /// </summary>
        /// <param name="serverId">
        ///     The Id of the server being reconfigured.
        /// </param>
        /// <param name="phase">
        ///     The current provisioning phase.
        /// </param>
        public ServerReconfiguring(int serverId, ServerProvisioningPhase phase)
            : base(serverId, ProvisioningStatus.Reconfiguring, phase)
        {
        }
    }
}
