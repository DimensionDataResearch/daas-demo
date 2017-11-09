using System;

namespace DaaSDemo.Provisioning.Messages
{
    /// <summary>
    ///     Subscribe to events (optionally, for the specified resource).
    /// </summary>
    public class SubscribeResourceEvents
    {
        /// <summary>
        ///     Create a new <see cref="SubscribeResourceEvents"/> message.
        /// </summary>
        /// <param name="resourceName">
        ///     An optional resource name (if specified, only subscribe to events relating to this resource).
        /// </param>
        public SubscribeResourceEvents(string resourceName = null)
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
