namespace DaaSDemo.Provisioning.Messages
{
    using Models.Data;

    /// <summary>
    ///     Message indicating that a database server is being de-provisioned.
    /// </summary>
    public class ServerDeprovisioning
            : ServerStatusChanged
    {
        /// <summary>
        ///     Create a new <see cref="ServerDeprovisioning"/> message.
        /// </summary>
        /// <param name="serverId">
        ///     The Id of the server being de-provisioned.
        /// </param>
        /// <param name="phase">
        ///     The current provisioning phase.
        /// </param>
        public ServerDeprovisioning(int serverId, ServerProvisioningPhase phase)
            : base(serverId, ProvisioningStatus.Deprovisioning, phase)
        {
        }
    }
}