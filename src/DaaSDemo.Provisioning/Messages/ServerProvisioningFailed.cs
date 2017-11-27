using System.Collections.Generic;

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
        ///     Create a new <see cref="ServerProvisioningFailed"/> message.
        /// </summary>
        /// <param name="serverId">
        ///     The Id of the server for which provisioning was unsuccessful.
        /// </param>
        /// <param name="messages">
        ///     Messages (if any) indicating the reason for the failure.
        /// </param>
        public ServerProvisioningFailed(string serverId, params string[] messages)
            : this(serverId, (IEnumerable<string>)messages)
        {
        }

        /// <summary>
        ///     Create a new <see cref="ServerProvisioningFailed"/> message.
        /// </summary>
        /// <param name="serverId">
        ///     The Id of the server for which provisioning was unsuccessful.
        /// </param>
        /// <param name="messages">
        ///     Messages (if any) indicating the reason for the failure.
        /// </param>
        public ServerProvisioningFailed(string serverId, IEnumerable<string> messages)
            : base(serverId, ProvisioningStatus.Error, messages)
        {
        }
    }
}
