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
        }
    }
}
