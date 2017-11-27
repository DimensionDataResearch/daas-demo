using System;
using System.Collections.Generic;
using System.Collections.Immutable;

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
        /// <param name="messages">
        ///     Messages (if any) associated with the status change.
        /// </param>
        protected ServerStatusChanged(string serverId, ProvisioningStatus status, IEnumerable<string> messages = null)
        {
            ServerId = serverId;
            Status = status;

            if (messages != null)
                Messages = Messages.AddRange(messages);
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
        /// <param name="messages">
        ///     Messages (if any) associated with the status change.
        /// </param>
        protected ServerStatusChanged(string serverId, ServerProvisioningPhase phase, IEnumerable<string> messages = null)
        {
            ServerId = serverId;
            Phase = phase;

            if (messages != null)
                Messages = Messages.AddRange(messages);
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
        /// <param name="messages">
        ///     Messages (if any) associated with the status change.
        /// </param>
        protected ServerStatusChanged(string serverId, ProvisioningStatus status, ServerProvisioningPhase phase, IEnumerable<string> messages = null)
        {
            ServerId = serverId;
            Status = status;
            Phase = phase;
            
            if (messages != null)
                Messages = Messages.AddRange(messages);
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

        /// <summary>
        ///     Messages (if any) associated with the status change.
        /// </summary>
        public ImmutableList<string> Messages { get; } = ImmutableList<string>.Empty;
    }
}
