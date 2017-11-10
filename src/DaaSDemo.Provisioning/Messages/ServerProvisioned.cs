using System;

namespace DaaSDemo.Provisioning.Messages
{
    using Models.Data;

    /// <summary>
    ///     Message indicating that a database server has been provisioned.
    /// </summary>
    public class ServerProvisioned
            : ServerStatusChanged
    {
        /// <summary>
        ///     Create a new <see cref="ServerProvisioned"/> message.
        /// </summary>
        /// <param name="serverId">
        ///     The Id of the server that was provisioned.
        /// </param>
        public ServerProvisioned(int serverId)
            : base(serverId, ProvisioningStatus.Ready)
        {
        }
    }
}
