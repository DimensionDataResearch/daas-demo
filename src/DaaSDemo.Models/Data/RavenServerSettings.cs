namespace DaaSDemo.Models.Data
{
    /// <summary>
    ///     Well-known settings for a <see cref="DatabaseServer"/> representing a RavenDB instance.
    /// </summary>
    public class RavenServerSettings
        : DatabaseServerSettings
    {
        /// <summary>
        ///     The server's X.509 certificate (in PKCS12 format).
        /// </summary>
        /// <remarks>
        ///     AF: Don't do this in real life, it makes kittens cry.
        ///         Instead, store the cert in Vault and just store the object Id here instead.
        /// </remarks>
        public byte[] ServerCertificatePkcs12 { get; set; }

        /// <summary>
        ///     The server's certificate password.
        /// </summary>
        public string ServerCertificatePassword { get; set; }

        /// <summary>
        ///     Create a deep clone of the <see cref="DatabaseServerSettings"/>.
        /// </summary>
        /// <returns>
        ///     The cloned <see cref="DatabaseServerSettings"/>.
        /// </returns>
        public override DatabaseServerSettings Clone()
        {
            return new RavenServerSettings
            {
                ServerCertificatePkcs12 = ServerCertificatePkcs12,
                ServerCertificatePassword = ServerCertificatePassword,
                Storage = Storage.Clone()
            };
        }
    }
}
