using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization;

namespace DaasDemo.KubeClient
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
        ///     The default Kubernetes namespace.
        /// </summary>
        public const string DefaultNamespace = "default";

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
            ReplicationControllersV1 = new ReplicationControllerClientV1(this);
        }

        /// <summary>
        ///     Dispose of resources being used by the <see cref="KubeApiClient"/>.
        /// </summary>
        public void Dispose() => Http?.Dispose();

        /// <summary>
        ///     The underlying HTTP client.
        /// </summary>
        internal HttpClient Http { get; }

        /// <summary>
        ///     The client for the ReplicationControllers (v1) API.
        /// </summary>
        public ReplicationControllerClientV1 ReplicationControllersV1 { get; }

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
    }
}