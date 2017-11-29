using Raven.Client.ServerWide;
using Raven.Client.ServerWide.Commands;
using Raven.Client.ServerWide.Operations;
using Raven.Client.ServerWide.Operations.Certificates;
using System;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace DaaSDemo.Data
{
    /// <summary>
    ///     Extension methods for the RavenDB client's <see cref="ServerOperationExecutor"/> and friends.
    /// </summary>
    public static class RavenServerOperationExtensions
    {
        /// <summary>
        ///     Get the names of all databases present on the server.
        /// </summary>
        /// <param name="serverOperations">
        ///     The server operations client.
        /// </param>
        /// <param name="start">
        ///     The index of the first name to return.
        /// </param>
        /// <param name="count">
        ///     The number of names to return.
        /// </param>
        /// <param name="cancellationToken">
        ///     An optional <see cref="CancellationToken"/> that can be used to cancel the request.
        /// </param>
        /// <returns>
        ///     An array of database names.
        /// </returns>
        public static Task<string[]> GetDatabaseNames(this ServerOperationExecutor serverOperations, int start, int count, CancellationToken cancellationToken = default)
        {
            if (serverOperations == null)
                throw new ArgumentNullException(nameof(serverOperations));
            
            return serverOperations.SendAsync(
                new GetDatabaseNamesOperation(start, count),
                cancellationToken
            );
        }

        /// <summary>
        ///     Create a database.
        /// </summary>
        /// <param name="serverOperations">
        ///     The server operations client.
        /// </param>
        /// <param name="name">
        ///     The name of the database to create.
        /// </param>
        /// <param name="cancellationToken">
        ///     An optional <see cref="CancellationToken"/> that can be used to cancel the request.
        /// </param>
        /// <returns>
        ///     A <see cref="DatabasePutResult"/> representing the operation result.
        /// </returns>
        public static Task<DatabasePutResult> CreateDatabase(this ServerOperationExecutor serverOperations, string name, CancellationToken cancellationToken = default)
        {
            if (serverOperations == null)
                throw new ArgumentNullException(nameof(serverOperations));

            if (String.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Argument cannot be null, empty, or entirely composed of whitespace: 'name'.", nameof(name));

            return serverOperations.SendAsync(
                new CreateDatabaseOperation(new DatabaseRecord
                {
                    DatabaseName = name
                }),
                cancellationToken
            );
        }

        /// <summary>
        ///     Delete a database.
        /// </summary>
        /// <param name="serverOperations">
        ///     The server operations client.
        /// </param>
        /// <param name="name">
        ///     The name of the database to delete.
        /// </param>
        /// <param name="hardDelete">
        ///     Delete the physical database files, too?
        /// </param>
        /// <param name="waitForConfirmation">
        ///     If specified, wait this long at most for confirmation of deletion.
        /// </param>
        /// <param name="cancellationToken">
        ///     An optional <see cref="CancellationToken"/> that can be used to cancel the request.
        /// </param>
        /// <returns>
        ///     A <see cref="DeleteDatabaseResult"/> representing the operation result.
        /// </returns>
        public static Task<DeleteDatabaseResult> DeleteDatabase(this ServerOperationExecutor serverOperations, string name, bool hardDelete = false, TimeSpan? waitForConfirmation = null, CancellationToken cancellationToken = default)
        {
            if (serverOperations == null)
                throw new ArgumentNullException(nameof(serverOperations));

            if (String.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Argument cannot be null, empty, or entirely composed of whitespace: 'name'.", nameof(name));

            return serverOperations.SendAsync(
                new DeleteDatabasesOperation(name, hardDelete, timeToWaitForConfirmation: waitForConfirmation),
                cancellationToken
            );
        }

        /// <summary>
        ///     Request creation of a client certificate for the specified user.
        /// </summary>
        /// <param name="serverOperations">
        ///     The server operations client.
        /// </param>
        /// <param name="subjectName">
        ///     The name of the security principal that the certificate will represent.
        /// </param>
        /// <param name="protectedWithPassword">
        ///     The password that the certificate will be protected with.
        /// </param>
        /// <param name="clearance">
        ///     Rights assigned to the user.
        /// </param>
        /// <param name="permissions">
        ///     Database-level permissions assigned to the user.
        /// </param>
        /// <param name="cancellationToken">
        ///     An optional <see cref="CancellationToken"/> that can be used to cancel the request.
        /// </param>
        /// <returns>
        ///     A byte array containing the PKCS12-encoded (i.e. PFX) certificate and private key.
        /// </returns>
        public static async Task<byte[]> CreateClientCertificate(this ServerOperationExecutor serverOperations, string subjectName, string protectedWithPassword, SecurityClearance clearance, Dictionary<string, DatabaseAccess> permissions = null, CancellationToken cancellationToken = default)
        {
            if (serverOperations == null)
                throw new ArgumentNullException(nameof(serverOperations));
            
            if (String.IsNullOrWhiteSpace(subjectName))
                throw new ArgumentException("Argument cannot be null, empty, or entirely composed of whitespace: 'userName'.", nameof(subjectName));
            
            CertificateRawData clientCertificatePfx = await serverOperations.SendAsync(
                new CreateClientCertificateOperation(
                    subjectName,
                    permissions ?? new Dictionary<string, DatabaseAccess>(),
                    clearance,
                    protectedWithPassword
                ),
                cancellationToken
            );

            return clientCertificatePfx.RawData;
        }

        /// <summary>
        ///     Register a client certificate for the specified user.
        /// </summary>
        /// <param name="serverOperations">
        ///     The server operations client.
        /// </param>
        /// <param name="subjectName">
        ///     The name of the security principal that the certificate will represent.
        /// </param>
        /// <param name="certificate">
        ///     The client certificate to register.
        /// </param>
        /// <param name="clearance">
        ///     Rights assigned to the user.
        /// </param>
        /// <param name="permissions">
        ///     Database-level permissions assigned to the user.
        /// </param>
        /// <param name="cancellationToken">
        ///     An optional <see cref="CancellationToken"/> that can be used to cancel the request.
        /// </param>
        /// <returns>
        ///     A <see cref="Task"/> representing the operation.
        /// </returns>
        public static async Task PutClientCertificate(this ServerOperationExecutor serverOperations, string subjectName, X509Certificate2 certificate, SecurityClearance clearance, Dictionary<string, DatabaseAccess> permissions = null, CancellationToken cancellationToken = default)
        {
            if (serverOperations == null)
                throw new ArgumentNullException(nameof(serverOperations));
            
            if (String.IsNullOrWhiteSpace(subjectName))
                throw new ArgumentException("Argument cannot be null, empty, or entirely composed of whitespace: 'userName'.", nameof(subjectName));

            if (certificate == null)
                throw new ArgumentNullException(nameof(certificate));
            
            await serverOperations.SendAsync(
                new PutClientCertificateOperation(
                    subjectName,
                    certificate,
                    permissions ?? new Dictionary<string, DatabaseAccess>(),
                    clearance
                ),
                cancellationToken
            );
        }

        /// <summary>
        ///     Retrieve details for a specific client certificate.
        /// </summary>
        /// <param name="serverOperations">
        ///     The server operations client.
        /// </param>
        /// <param name="thumbprint">
        ///     The thumbprint of the certificate to retrieve.
        /// </param>
        /// <param name="cancellationToken">
        ///     An optional <see cref="CancellationToken"/> that can be used to cancel the request.
        /// </param>
        /// <returns>
        ///     A <see cref="CertificateDefinition"/> representing the certificate, or <c>null</c> if no certificate was found with the specified thumbprint.
        /// </returns>
        public static Task<CertificateDefinition> GetClientCertificate(this ServerOperationExecutor serverOperations, string thumbprint, CancellationToken cancellationToken = default)
        {
            if (serverOperations == null)
                throw new ArgumentNullException(nameof(serverOperations));
            
            if (String.IsNullOrWhiteSpace(thumbprint))
                throw new ArgumentException("Argument cannot be null, empty, or entirely composed of whitespace: 'userName'.", nameof(thumbprint));
            
            return serverOperations.SendAsync(
                new GetCertificateOperation(thumbprint),
                cancellationToken
            );
        }

        /// <summary>
        ///     Retrieve details for all client certificates.
        /// </summary>
        /// <param name="serverOperations">
        ///     The server operations client.
        /// </param>
        /// <param name="start">
        ///     The index of the first certificate to return.
        /// </param>
        /// <param name="count">
        ///     The number of certificates to return.
        /// </param>
        /// <param name="cancellationToken">
        ///     An optional <see cref="CancellationToken"/> that can be used to cancel the request.
        /// </param>
        /// <returns>
        ///     An array of <see cref="CertificateDefinition"/>s representing the certificates.
        /// </returns>
        public static Task<CertificateDefinition[]> GetClientCertificates(this ServerOperationExecutor serverOperations, int start, int count, CancellationToken cancellationToken = default)
        {
            if (serverOperations == null)
                throw new ArgumentNullException(nameof(serverOperations));
            
            return serverOperations.SendAsync(
                new GetCertificatesOperation(start, count),
                cancellationToken
            );
        }

        /// <summary>
        ///     Delete a client certificate.
        /// </summary>
        /// <param name="serverOperations">
        ///     The server operations client.
        /// </param>
        /// <param name="thumbprint">
        ///     The thumbprint of the certificate to delete.
        /// </param>
        /// <param name="cancellationToken">
        ///     An optional <see cref="CancellationToken"/> that can be used to cancel the request.
        /// </param>
        /// <returns>
        ///     A <see cref="Task"/> representing the operation.
        /// </returns>
        public static Task DeleteClientCertificate(this ServerOperationExecutor serverOperations, string thumbprint, CancellationToken cancellationToken = default)
        {
            if (serverOperations == null)
                throw new ArgumentNullException(nameof(serverOperations));
            
            if (String.IsNullOrWhiteSpace(thumbprint))
                throw new ArgumentException("Argument cannot be null, empty, or entirely composed of whitespace: 'userName'.", nameof(thumbprint));
            
            return serverOperations.SendAsync(
                new DeleteCertificateOperation(thumbprint),
                cancellationToken
            );
        }
    }
}
