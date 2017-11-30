namespace DaaSDemo.Models.Data
{
    /// <summary>
    ///     Represents an X.509 certificate used to authenticate a database user.
    /// </summary>
    public sealed class DatabaseUserClientCertificate
        : DatabaseUserCredential
    {
        /// <summary>
        ///     A <see cref="DatabaseUserCredentialKind"/> value that indicates the credential type.
        /// </summary>
        public override DatabaseUserCredentialKind Kind => DatabaseUserCredentialKind.Certificate;

        /// <summary>
        ///     The certificate subject name.
        /// </summary>
        public string Subject { get; set; }

        /// <summary>
        ///     The certificate thumbprint.
        /// </summary>
        public string Thumbprint { get; set; }

        /// <summary>
        ///     The PKCS12-encoded certificate data (including the private key).
        /// </summary>
        public byte[] CertificatePkcs12 { get; set; }

        /// <summary>
        ///     The password needed to decrypt the certificate data.
        /// </summary>
        public string CertificatePassword { get; set; }

        /// <summary>
        ///     Create a deep clone of the <see cref="DatabaseUserClientCertificate"/>.
        /// </summary>
        /// <returns>
        ///     The cloned <see cref="DatabaseUserClientCertificate"/>.
        /// </returns>
        public override DatabaseUserCredential Clone()
        {
            return new DatabaseUserClientCertificate
            {
                Subject = Subject,
                Thumbprint = Thumbprint,
                CertificatePkcs12 = (byte[])CertificatePkcs12?.Clone(),
                CertificatePassword = CertificatePassword
            };
        }
    }
}
