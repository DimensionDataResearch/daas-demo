namespace DaaSDemo.Provisioning.Messages
{

    /// <summary>
    ///     Unsubscribe from events for all resources.
    /// </summary>
    public class UnsubscribeAllResourceEvents
    {
        /// <summary>
        ///     The singleton instance of the <see cref="UnsubscribeAllResourceEvents"/>.
        /// </summary>
        public static UnsubscribeAllResourceEvents Instance = new UnsubscribeAllResourceEvents();

        /// <summary>
        ///     Create a new <see cref="UnsubscribeAllResourceEvents"/> message.
        /// </summary>
        UnsubscribeAllResourceEvents()
        {
        }
    }
}
