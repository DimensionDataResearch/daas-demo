namespace DaaSDemo.Models.Data
{
    /// <summary>
    ///     A provisioning action to be performed for a resource.
    /// </summary>
    public enum ProvisioningAction
    {
        /// <summary>
        ///     No provisioning action.
        /// </summary>
        None = 0,

        /// <summary>
        ///     Provision resource(s).
        /// </summary>
        Provision = 1,

        /// <summary>
        ///     De-provision resource(s).
        /// </summary>
        Deprovision = 2,

        /// <summary>
        ///     Reconfigure resource(s).
        /// </summary>
        Reconfigure = 3
    }
}
