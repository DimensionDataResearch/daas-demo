namespace DaaSDemo.Models.Data
{
    /// <summary>
    ///     Well-known types of event relating to a <see cref="DatabaseServer"/>.
    /// </summary>
    public enum DatabaseServerEventKind
    {
        /// <summary>
        ///     A provisioning-related event.
        /// </summary>
        Provisioning,

        /// <summary>
        ///     Event indicating that a server's ingress details have changed.
        /// </summary>
        IngressChanged
    }
}
