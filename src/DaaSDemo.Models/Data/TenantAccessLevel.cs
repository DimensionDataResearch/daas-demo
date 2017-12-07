namespace DaaSDemo.Models.Data
{
    /// <summary>
    ///     Represents a user's level of access to a tenant.
    /// </summary>
    public enum TenantAccessLevel
    {
        /// <summary>
        ///     User has read-only access to the tenant's resources.
        /// </summary>
        Read = 1,

        /// <summary>
        ///     User has read / write access to the tenant's resources.
        /// </summary>
        ReadWrite = 2,

        /// <summary>
        ///     User has full control of the tenant.
        /// </summary>
        Owner = 3
    }
}
