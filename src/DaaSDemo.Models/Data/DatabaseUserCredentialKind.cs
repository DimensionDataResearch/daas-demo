namespace DaaSDemo.Models.Data
{
    /// <summary>
    ///     Well-known types of database user credentials.
    /// </summary>
    public enum DatabaseUserCredentialKind
    {
        /// <summary>
        ///     A password.
        /// </summary>
        Password = 1,

        /// <summary>
        ///     An X.509 certificate (including private key).
        /// </summary>
        Certificate = 2
    }
}
