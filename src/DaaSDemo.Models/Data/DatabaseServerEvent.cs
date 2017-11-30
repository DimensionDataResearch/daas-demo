using System;
using System.Collections.Generic;

namespace DaaSDemo.Models.Data
{
    /// <summary>
    ///     The base class for events relating to a <see cref="DatabaseServer"/>.
    /// </summary>
    public abstract class DatabaseServerEvent
        : IDeepCloneable<DatabaseServerEvent>
    {
        /// <summary>
        ///     The date / time that the event occurred.
        /// </summary>
        public DateTimeOffset Timestamp { get; set; }

        /// <summary>
        ///     Messages (if any) relating to the event.
        /// </summary>
        public List<string> Messages { get; protected set; } = new List<string>();

        /// <summary>
        ///     The kind of event represented by the <see cref="DatabaseServerEvent"/>.
        /// </summary>
        public abstract DatabaseServerEventKind Kind { get; }

        /// <summary>
        ///     Perform a deep clone of the <see cref="DatabaseServerEvent"/>.
        /// </summary>
        /// <returns>
        ///     The cloned <see cref="DatabaseServerEvent"/>.
        /// </returns>
        public abstract DatabaseServerEvent Clone();
    }
}
