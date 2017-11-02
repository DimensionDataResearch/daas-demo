namespace DaaSDemo.Provisioning.Messages
{
    using Data.Models;

    /// <summary>
    ///     Message indicating that a database server is being provisioned.
    /// </summary>
    public class ServerProvisioning
        : ServerStatusChanged
    {
        /// <summary>
        ///     Create a new <see cref="ServerProvisioning"/> message.
        /// </summary>
        /// <param name="serverId">
        ///     The Id of the server being provisioned.
        /// </param>
        /// <param name="phase">
        ///     The current provisioning phase.
        /// </param>
        public ServerProvisioning(int serverId, ServerProvisioningPhase phase)
            : base(serverId, ProvisioningStatus.Provisioning, phase)
        {
        }
    }
}