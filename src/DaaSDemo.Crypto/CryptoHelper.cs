using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.X509;
using Org.BouncyCastle.Security;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using VaultSharp.Backends.Secret.Models.PKI;

using X509Certificate = Org.BouncyCastle.X509.X509Certificate;
using System.Security.Cryptography;

namespace DaaSDemo.Crypto
{
    /// <summary>
    ///     Cryptographic helper functions.
    /// </summary>
    public static class CryptoHelper
    {
        /// <summary>
        ///     Convert Vault certificate credentials to an <see cref="X509Certificate2"/> (with private key, if present).
        /// </summary>
        /// <param name="certificateCredentials">
        ///     The Vault <see cref="CertificateCredentials"/> to convert.
        /// </param>
        /// <param name="keyStorageFlags">
        ///     Optional key-storage flags that control how the private key is persisted.
        /// </param>
        /// <returns>
        ///     The certificate, as an <see cref="X509Certificate2"/>.
        /// </returns>
        public static X509Certificate2 ToX509Certificate(this CertificateCredentials certificateCredentials, X509KeyStorageFlags keyStorageFlags = X509KeyStorageFlags.DefaultKeySet | X509KeyStorageFlags.Exportable)
        {
            if (certificateCredentials == null)
                throw new ArgumentNullException(nameof(certificateCredentials));
            
            // TODO: Use CRNG.
            string tempPassword = Guid.NewGuid().ToString("N");
            byte[] pfxData = certificateCredentials.ToPfx(tempPassword);
            
            return new X509Certificate2(pfxData, tempPassword, keyStorageFlags);
        }

        /// <summary>
        ///     Convert Vault certificate credentials to PFX / PKCS12 format (with private key, if present).
        /// </summary>
        /// <param name="certificateCredentials">
        ///     The Vault <see cref="CertificateCredentials"/> to convert.
        /// </param>
        /// <param name="password">
        ///     The password to use for protecting the exported data.
        /// </param>
        /// <returns>
        ///     A byte array containing the exported data.
        /// </returns>
        public static byte[] ToPfx(this CertificateCredentials certificateCredentials, string password)
        {
            if (certificateCredentials == null)
                throw new ArgumentNullException(nameof(certificateCredentials));

            if (String.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Argument cannot be null, empty, or entirely composed of whitespace: 'password'.", nameof(password));

            X509CertificateEntry[] chain = new X509CertificateEntry[1];
            AsymmetricCipherKeyPair privateKey = null;

            foreach (object pemObject in EnumeratePemObjects(password, certificateCredentials.CertificateContent, certificateCredentials.PrivateKey))
            {
                if (pemObject is X509Certificate certificate)
                    chain[0] = new X509CertificateEntry(certificate);
                else if (pemObject is AsymmetricCipherKeyPair keyPair)
                    privateKey = keyPair;
            }

            if (chain[0] == null)
                throw new CryptographicException("Cannot find X.509 certificate in PEM data.");

            if (privateKey == null)
                throw new CryptographicException("Cannot find private key in PEM data.");

            string certificateSubject = chain[0].Certificate.SubjectDN.ToString();

            Pkcs12Store store = new Pkcs12StoreBuilder().Build();
            store.SetKeyEntry(certificateSubject, new AsymmetricKeyEntry(privateKey.Private), chain);

            using (MemoryStream pfxData = new MemoryStream())
            {
                store.Save(pfxData, password.ToCharArray(), new SecureRandom());

                return pfxData.ToArray();
            }
        }

        /// <summary>
        ///     Enumerate the objects in one or more PEM-encoded blocks.
        /// </summary>
        /// <param name="pemPassword">
        ///     The password used to protect the encoded data.
        /// </param>
        /// <param name="pemBlocks">
        ///     The PEM-encoded blocks.
        /// </param>
        /// <returns>
        ///     A sequence of BouncyCastle cryptographic objects (e.g. <see cref="X509Certificate"/>, <see cref="AsymmetricCipherKeyPair"/>, etc).
        /// </returns>
        public static IEnumerable<object> EnumeratePemObjects(string pemPassword, params string[] pemBlocks) => EnumeratePemObjects(pemPassword, (IEnumerable<string>)pemBlocks);

        /// <summary>
        ///     Enumerate the objects in one or more PEM-encoded blocks.
        /// </summary>
        /// <param name="pemPassword">
        ///     The password used to protect the encoded data.
        /// </param>
        /// <param name="pemBlocks">
        ///     The PEM-encoded blocks.
        /// </param>
        /// <returns>
        ///     A sequence of BouncyCastle cryptographic objects (e.g. <see cref="X509Certificate"/>, <see cref="AsymmetricCipherKeyPair"/>, etc).
        /// </returns>
        public static IEnumerable<object> EnumeratePemObjects(string pemPassword, IEnumerable<string> pemBlocks)
        {
            if (pemBlocks == null)
                throw new ArgumentNullException(nameof(pemBlocks));

            if (String.IsNullOrWhiteSpace(pemPassword))
                throw new ArgumentException("Argument cannot be null, empty, or entirely composed of whitespace: 'password'.", nameof(pemPassword));

            foreach (string pemBlock in pemBlocks)
            {
                if (String.IsNullOrWhiteSpace(pemBlock))
                    continue;

                PemReader pemReader = new PemReader(
                    new StringReader(pemBlock),
                    new StaticPasswordStore(pemPassword)
                );

                object pemObject;
                while ((pemObject = pemReader.ReadObject()) != null)
                    yield return pemObject;
                }
        }

        /// <summary>
        ///     A static implementation of <see cref="IPasswordFinder"/> used to feed the decryption password to BouncyCastle.
        /// </summary>
        class StaticPasswordStore
            : IPasswordFinder
        {
            /// <summary>
            ///     The decryption password.
            /// </summary>
            readonly string _password;

            /// <summary>
            ///     Create a new <see cref="StaticPasswordStore"/>.
            /// </summary>
            /// <param name="password">
            ///     The decryption password.
            /// </param>
            public StaticPasswordStore(string password)
            {
                if (String.IsNullOrWhiteSpace(password))
                    throw new ArgumentException("Argument cannot be null, empty, or entirely composed of whitespace: 'password'.", nameof(password));

                _password = password;
            }

            /// <summary>
            ///     Get a copy of the decryption password.
            /// </summary>
            /// <returns>
            ///     An array of characters representing the decryption password.
            /// </returns>
            public char[] GetPassword() => _password.ToCharArray();
        }
    }
}
