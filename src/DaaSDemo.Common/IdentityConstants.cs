namespace DaaSDemo.Common
{
    /// <summary>
    ///     DaaS Identity constants.
    /// </summary>
    public static class IdentityConstants
    {
        /// <summary>
        ///     Well-known JWT claim types used by DaaS Identity.
        /// </summary>
        public static class JwtClaimTypes
        {
            /// <summary>
            ///     Is the principal a super-user.
            /// </summary>
            public static readonly string SuperUser = "su";

            /// <summary>
            ///     The Id of a tenant that the principal has read-level access to.
            /// </summary>
            public static readonly string TenantAccessRead = "tenant.read";

            /// <summary>
            ///     The Id of a tenant that the principal has read-level and write-level access to.
            /// </summary>
            public static readonly string TenantAccessReadWrite = "tenant.readwrite";

            /// <summary>
            ///     The Id of a tenant that the principal has owner-level access to.
            /// </summary>
            public static readonly string TenantAccessOwner = "tenant.owner";
        }
    }
}
