namespace DaaSDemo.Models.Data
{
    /// <summary>
    ///     The provisioning status of a resource.
    /// </summary>
    public enum ProvisioningStatus
    {
        /// <summary>
        ///     Resource provisioning is pending.
        /// </summary>
        Pending = 0,

        /// <summary>
        ///     Resource is ready for use.
        /// </summary>
        Ready = 1,

        /// <summary>
        ///     Resource is being provisioned.
        /// </summary>
        Provisioning = 2,

        /// <summary>
        ///     Resource is being de-provisioned.
        /// </summary>
        Deprovisioning = 3,

        /// <summary>
        ///     Resource is being reconfigured.
        /// </summary>
        Reconfiguring = 4,

        /// <summary>
        ///     Resource state is invalid.
        /// </summary>
        Error = 5,

        /// <summary>
        ///     Resource has been de-provisioned.
        /// </summary>
        Deprovisioned = 6
    }
}
