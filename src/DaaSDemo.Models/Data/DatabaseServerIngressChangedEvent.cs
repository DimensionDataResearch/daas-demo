using System.Collections.Generic;

namespace DaaSDemo.Models.Data
{
    /// <summary>
    ///     Event indicating that a <see cref="DatabaseServer"/>'s ingress details have changed.
    /// </summary>
    public class DatabaseServerIngressChangedEvent
        : DatabaseServerEvent
    {
        /// <summary>
        ///     The server's current fully-qualified public domain name (if any).
        /// </summary>
        public string PublicFQDN { get; set; }

        /// <summary>
        ///     The server's current public TCP port (if any).
        /// </summary>
        public int? PublicPort { get; set; }

        /// <summary>
        ///     The kind of event represented by the <see cref="DatabaseServerEvent"/>.
        /// </summary>
        public override DatabaseServerEventKind Kind => DatabaseServerEventKind.IngressChanged;

        /// <summary>
        ///     Perform a deep clone of the <see cref="DatabaseServerEvent"/>.
        /// </summary>
        /// <returns>
        ///     The cloned <see cref="DatabaseServerEvent"/>.
        /// </returns>
        public override DatabaseServerEvent Clone()
        {
            return new DatabaseServerIngressChangedEvent
            {
                Timestamp = Timestamp,
                Messages = new List<string>(Messages),

                PublicFQDN = PublicFQDN,
                PublicPort = PublicPort
            };
        }
    }
}
