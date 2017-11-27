using HTTPlease;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VaultSharp;
using VaultSharp.Backends.Secret.Models.PKI;

namespace DaaSDemo.Provisioning.Provisioners
{
    using Common.Options;
    using Crypto;
    using Models.Data;
    using KubeClient;
    using KubeClient.Models;

    /// <summary>
    ///     Provisioning facility for Kubernetes Secrets from Vault certificate credentials.
    /// </summary>
    public class ServerCredentialsProvisioner
        : Provisioner
    {
        /// <summary>
        ///     Create a new <see cref="ServerCredentialsProvisioner"/>.
        /// </summary>
        /// <param name="logger">
        ///     The provisioner's logger.
        /// </param>
        /// <param name="kubeClient">
        ///     The <see cref="KubeApiClient"/> used to communicate with the Kubernetes API.
        /// </param>
        /// <param name="vaultClient">
        ///     The <see cref="IVaultClient"/> used to communicate with the Vault API.
        /// </param>
        /// <param name="kubeResources">
        ///     A factory for Kubernetes resource models.
        /// </param>
        /// <param name="vaultOptions">
        ///     Application-level Vault settings.
        /// </param>
        /// <param name="kubeOptions">
        ///     Application-level Kubernetes settings.
        /// </param>
        public ServerCredentialsProvisioner(ILogger<DatabaseServerProvisioner> logger, KubeApiClient kubeClient, IVaultClient vaultClient, KubeResources kubeResources, IOptions<VaultOptions> vaultOptions, IOptions<KubernetesOptions> kubeOptions)
            : base(logger)
        {
            if (kubeClient == null)
                throw new ArgumentNullException(nameof(kubeClient));
            
            if (vaultClient == null)
                throw new ArgumentNullException(nameof(vaultClient));

            if (kubeResources == null)
                throw new ArgumentNullException(nameof(kubeResources));

            if (vaultOptions == null)
                throw new ArgumentNullException(nameof(vaultOptions));

            if (kubeOptions == null)
                throw new ArgumentNullException(nameof(kubeOptions));
            
            KubeClient = kubeClient;
            VaultClient = vaultClient;

            KubeResources = kubeResources;
            
            VaultOptions = vaultOptions.Value;
            KubeOptions = kubeOptions.Value;
        }

        /// <summary>
        ///     The server's current state (if known).
        /// </summary>
        public DatabaseServer State { get; set; }

        /// <summary>
        ///     The <see cref="KubeApiClient"/> used to communicate with the Kubernetes API.
        /// </summary>
        KubeApiClient KubeClient { get; }

        /// <param name="vaultClient">
        ///     The <see cref="IVaultClient"/> used to communicate with the Vault API.
        /// </param>
        IVaultClient VaultClient { get; }

        /// <summary>
        ///     A factory for Kubernetes resource models.
        /// </summary>
        KubeResources KubeResources { get; }

        /// <summary>
        ///     Application-level Kubernetes settings.
        /// </summary>
        KubernetesOptions KubeOptions { get; }

        /// <summary>
        ///     Application-level Vault settings.
        /// </summary>
        VaultOptions VaultOptions { get; }

        /// <summary>
        ///     Find the server's associated Secret for credentials.
        /// </summary>
        /// <returns>
        ///     The Secret, or <c>null</c> if it was not found.
        /// </returns>
        public async Task<SecretV1> FindCredentialsSecret()
        {
            RequireCurrentState();

            List<SecretV1> matchingSecrets = await KubeClient.SecretsV1().List(
                labelSelector: $"cloud.dimensiondata.daas.server-id = {State.Id}, cloud.dimensiondata.daas.secret-type = credentials",
                kubeNamespace: KubeOptions.KubeNamespace
            );

            if (matchingSecrets.Count == 0)
                return null;

            return matchingSecrets[matchingSecrets.Count - 1];
        }

        /// <summary>
        ///     Ensure that a Secret for data exists for the specified database server.
        /// </summary>
        /// <returns>
        ///     The Secret resource, as a <see cref="SecretV1"/>.
        /// </returns>
        public async Task<SecretV1> EnsureCredentialsSecretPresent()
        {
            RequireCurrentState();

            SecretV1 existingSecret = await FindCredentialsSecret();
            if (existingSecret != null)
            {
                Log.LogInformation("Found existing credentials secret {SecretName} for server {ServerId}.",
                    existingSecret.Metadata.Name,
                    State.Id
                );

                return existingSecret;
            }

            Log.LogInformation("Creating credentials secret for server {ServerId}...",
                State.Id
            );

            Log.LogInformation("Requesting X.509 certificate...");

            CertificateCredentials serverCertificate = await RequestServerCertificate();

            SecretV1 createdSecret = await KubeClient.SecretsV1().Create(
                KubeResources.CredentialsSecret(State, serverCertificate,
                    kubeNamespace: KubeOptions.KubeNamespace
                )
            );

            Log.LogInformation("Successfully created credentials secret {SecretName} for server {ServerId}.",
                createdSecret.Metadata.Name,
                State.Id
            );

            return createdSecret;
        }

        /// <summary>
        ///     Ensure that a Secret for credentials does not exist for the specified database server.
        /// </summary>
        /// <returns>
        ///     <c>true</c>, if the controller is now absent; otherwise, <c>false</c>.
        /// </returns>
        public async Task<bool> EnsureCredentialsSecretAbsent()
        {
            RequireCurrentState();

            SecretV1 credentialsSecret = await FindCredentialsSecret();
            if (credentialsSecret == null)
                return true;

            Log.LogInformation("Deleting credentials secret {SecretName} for server {ServerId}...",
                credentialsSecret.Metadata.Name,
                State.Id
            );

            try
            {
                await KubeClient.SecretsV1().Delete(
                    name: credentialsSecret.Metadata.Name,
                    kubeNamespace: KubeOptions.KubeNamespace
                );
            }
            catch (HttpRequestException<StatusV1> deleteFailed)
            {
                Log.LogError("Failed to delete credentials secret {SecretName} for server {ServerId} (Message:{FailureMessage}, Reason:{FailureReason}).",
                    credentialsSecret.Metadata.Name,
                    State.Id,
                    deleteFailed.Response.Message,
                    deleteFailed.Response.Reason
                );

                return false;
            }

            Log.LogInformation("Deleted credentials secret {SecretName} for server {ServerId}.",
                credentialsSecret.Metadata.Name,
                State.Id
            );

            return true;
        }

        /// <summary>
        ///     Request an X.509 certificate for the database server.
        /// </summary>
        /// <returns>
        ///     <see cref="CertificateCredentials"> representing the certificate.
        /// </returns>
        async Task<CertificateCredentials> RequestServerCertificate()
        {
            RequireCurrentState();

            string subjectName = $"database.{KubeOptions.ClusterPublicFQDN}";
            string[] subjectAlternativeNames = new string[]
            {
                $"{State.Name}.database.{KubeOptions.ClusterPublicFQDN}"
                // TODO: Add SAN for server's internal Service FQDN'.
            };

            Log.LogInformation("Requesting server certificate for {ServerId} (Subject = {SubjectName} , SANs = {@SubjectAlternativeNames}).",
                State.Id,
                subjectName,
                subjectAlternativeNames
            );

            var credentials = await VaultClient.PKIGenerateDynamicCredentialsAsync(
                VaultOptions.CertificatePolicyName,
                new CertificateCredentialsRequestOptions
                {
                    CertificateFormat = CertificateFormat.pem,
                    CommonName = subjectName,
                    SubjectAlternativeNames = String.Join(",", subjectAlternativeNames),
                    TimeToLive = "672h"
                },
                VaultOptions.PkiBasePath
            );

            Log.LogInformation("Acquired server certificate for {ServerId}.",
                State.Id
            );

            return credentials.Data;
        }

        /// <summary>
        ///     Ensure that <see cref="State"/> is populated.
        /// </summary>
        void RequireCurrentState()
        {
            if (State == null)
                throw new InvalidOperationException($"Cannot use {nameof(ServerCredentialsProvisioner)} without current state.");
        }
    }
}
