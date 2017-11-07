using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization;
using System.Security.Cryptography.X509Certificates;

namespace DaaSDemo.KubeClient
{
    using Clients;
    using Models;

    /// <summary>
    ///     Client for the Kubernetes API.
    /// </summary>
    public sealed class KubeApiClient
        : IDisposable
    {
        /// <summary>
        ///     Create a new <see cref="KubeApiClient"/>.
        /// </summary>
        /// <param name="httpClient">
        ///     The underlying HTTP client.
        /// </param>
        KubeApiClient(HttpClient httpClient)
        {
            if (httpClient == null)
                throw new ArgumentNullException(nameof(httpClient));

            Http = httpClient;
            SecretsV1 = new SecretClientV1(this);
            ConfigMapsV1 = new ConfigMapClientV1(this);
            PodsV1 = new PodClientV1(this);
            ReplicationControllersV1 = new ReplicationControllerClientV1(this);
            ServicesV1 = new ServiceClientV1(this);
            JobsV1 = new JobClientV1(this);
            VoyagerIngressesV1Beta1 = new VoyagerIngressClientV1Beta1(this);
        }

        /// <summary>
        ///     Dispose of resources being used by the <see cref="KubeApiClient"/>.
        /// </summary>
        public void Dispose() => Http?.Dispose();

        /// <summary>
        ///     The default Kubernetes namespace.
        /// </summary>
        public string DefaultNamespace { get; set; } = "default";

        /// <summary>
        ///     The underlying HTTP client.
        /// </summary>
        public HttpClient Http { get; }

        /// <summary>
        ///     The client for the Secrets (v1) API.
        /// </summary>
        public SecretClientV1 SecretsV1 { get; }

        /// <summary>
        ///     The client for the ConfigMaps (v1) API.
        /// </summary>
        public ConfigMapClientV1 ConfigMapsV1 { get; }

        /// <summary>
        ///     The client for the Pods (v1) API.
        /// </summary>
        public PodClientV1 PodsV1 { get; }

        /// <summary>
        ///     The client for the ReplicationControllers (v1) API.
        /// </summary>
        public ReplicationControllerClientV1 ReplicationControllersV1 { get; }

        /// <summary>
        ///     The client for the Services (v1) API.
        /// </summary>
        public ServiceClientV1 ServicesV1 { get; }

        /// <summary>
        ///     The client for the Services (v1) API.
        /// </summary>
        public JobClientV1 JobsV1 { get; }

        /// <summary>
        ///     The client for the Voyager Ingress (v1beta1) API.
        /// </summary>
        public VoyagerIngressClientV1Beta1 VoyagerIngressesV1Beta1 { get; }

        /// <summary>
        ///     Create a new <see cref="KubeApiClient"/>.
        /// </summary>
        /// <param name="endPointUri">
        ///     The base address for the Kubernetes API end-point.
        /// </param>
        /// <param name="accessToken">
        ///     The access token to use for authentication to the API.
        /// </param>
        /// <returns>
        ///     The configured <see cref="KubeApiClient"/>.
        /// </returns>
        public static KubeApiClient Create(Uri endPointUri, string accessToken)
        {
            if (endPointUri == null)
                throw new ArgumentNullException(nameof(endPointUri));
            
            if (String.IsNullOrWhiteSpace(accessToken))
                throw new ArgumentException("Argument cannot be null, empty, or entirely composed of whitespace: 'accessToken'.", nameof(accessToken));

            return new KubeApiClient(
                new HttpClient
                {
                    BaseAddress = endPointUri,
                    DefaultRequestHeaders =
                    {
                        Authorization = new AuthenticationHeaderValue(
                            scheme: "Bearer",
                            parameter: accessToken
                        )
                    }
                }
            );
        }

        /// <summary>
        ///     Create a new <see cref="KubeApiClient"/> using pod-level configuration.
        /// </summary>
        /// <returns>
        ///     The configured <see cref="KubeApiClient"/>.
        /// </returns>
        /// <remarks>
        ///     Only works from within a container running in a Kubernetes Pod.
        /// </remarks>
        public static KubeApiClient CreateFromPodServiceAccount()
        {
            var caCertificate = new X509Certificate2(
                File.ReadAllBytes("/var/run/secrets/kubernetes.io/serviceaccount/ca.crt")
            );

            HttpClientHandler clientHandler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (request, certificate, _, errors) =>
                {
                    // TODO: Work out how to verify remote certificate (X509Chain doesn't seem to work correctly on Linux).

                    return true;
                }
            };

            return new KubeApiClient(
                new HttpClient(clientHandler)
                {
                    BaseAddress = new Uri("https://kubernetes/"),
                    DefaultRequestHeaders =
                    {
                        Authorization = new AuthenticationHeaderValue(
                            scheme: "Bearer",
                            parameter: File.ReadAllText("/var/run/secrets/kubernetes.io/serviceaccount/token")
                        )
                    }
                }
            );
        }
    }
}
