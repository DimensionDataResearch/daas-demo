using System;

namespace DaaSDemo.Provisioning.Messages
{
    /// <summary>
    ///     Unsubscribe from events (optionally, for the specified resource).
    /// </summary>
    public class UnsubscribeResourceEvents
    {
        /// <summary>
        ///     Create a new <see cref="UnsubscribeResourceEvents"/> message.
        /// </summary>
        /// <param name="resourceName">
        ///     An optional resource name (if specified, only unsubscribe from events relating to this resource).
        /// </param>
        public UnsubscribeResourceEvents(string resourceName = null)
        {
            ResourceName = resourceName ?? String.Empty;
        }

        /// <summary>
        ///     The name of a specific target resource.
        /// </summary>
        /// <remarks>
        ///     If <see cref="String.Empty"/>, then all resource events will be subscribed to.
        /// </remarks>
        public string ResourceName { get; }
    }
}
