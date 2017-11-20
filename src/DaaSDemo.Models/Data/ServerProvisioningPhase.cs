namespace DaaSDemo.Models.Data
{
    /// <summary>
    ///     Represents a phase in server provisioning / reconfiguration / de-provisioning.
    /// </summary>
    public enum ServerProvisioningPhase
    {
        /// <summary>
        ///     No provisioning phase is currently active.
        /// </summary>
        None = 0,

        /// <summary>
        ///     The server's storage (volumes etc).
        /// </summary>
        Storage = 1,

        /// <summary>
        ///     The server instance.
        /// </summary>
        Instance = 2,

        /// <summary>
        ///     The server's internal network connectivity.
        /// </summary>
        Network = 3,

        /// <summary>
        ///     The server's monitoring infrastructure.
        /// </summary>
        Monitoring = 4,

        /// <summary>
        ///     The server's configuration.
        /// </summary>
        Configuration = 5,

        /// <summary>
        ///     The server's external network connectivity.
        /// </summary>
        Ingress = 6,

        /// <summary>
        ///     The server's current action has been completed.
        /// </summary>
        Done = 7
    }
}
