namespace DaaSDemo.Models.Api
{
    /// <summary>
    ///     Information about a tenant that a user has access to.
    /// </summary>
    /// <remarks>
    ///     TODO: Use this model to replace XXXTenantIds properties on UserDetail with XXXTenants equivalents (remember to update the index definition).
    /// </remarks>
    public class UserTenantDetail
    {
        /// <summary>
        ///     The tenant Id.
        /// </summary>
        public string TenantId { get; set; }

        /// <summary>
        ///     The tenant name.
        /// </summary>
        public string TenantName { get; set; }
    }
}
