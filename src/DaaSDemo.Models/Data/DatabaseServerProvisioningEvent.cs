using System.Collections.Generic;

namespace DaaSDemo.Models.Data
{
    /// <summary>
    ///     A provisioning event related to a <see cref="DatabaseServer"/>.
    /// </summary>
    public class DatabaseServerProvisioningEvent
        : DatabaseServerEvent
    {
        /// <summary>
        ///     The requested action.
        /// </summary>
        public ProvisioningAction Action { get; set; }

        /// <summary>
        ///     The current provisioning phase (if any).
        /// </summary>
        public ServerProvisioningPhase Phase { get; set; }

        /// <summary>
        ///     The current status.
        /// </summary>
        public ProvisioningStatus Status { get; set; }

        /// <summary>
        ///     The kind of event represented by the <see cref="DatabaseServerEvent"/>.
        /// </summary>
        public override DatabaseServerEventKind Kind => DatabaseServerEventKind.Provisioning;

        /// <summary>
        ///     Perform a deep clone of the <see cref="DatabaseServerEvent"/>.
        /// </summary>
        /// <returns>
        ///     The cloned <see cref="DatabaseServerEvent"/>.
        /// </returns>
        public override DatabaseServerEvent Clone()
        {
            return new DatabaseServerProvisioningEvent
            {
                Timestamp = Timestamp,
                Messages = new List<string>(Messages),

                Action = Action,
                Phase = Phase,
                Status = Status,
            };
        }
    }
}
