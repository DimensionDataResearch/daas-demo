using HTTPlease;
using KubeNET.Swagger.Model;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace DaaSDemo.KubeClient.Clients
{
    /// <summary>
    ///     The base class for Kubernetes resource API clients.
    /// </summary>
    public abstract class KubeResourceClient
    {
        /// <summary>
        ///     Create a new <see cref="KubeResourceClient"/>.
        /// </summary>
        /// <param name="client">
        ///     The Kubernetes API client.
        /// </param>
        public KubeResourceClient(KubeApiClient client)
        {
            if (client == null)
                throw new ArgumentNullException(nameof(client));
            
            Client = client;
        }

        /// <summary>
        ///     The Kubernetes API client.
        /// </summary>
        protected KubeApiClient Client { get; }

        /// <summary>
        ///     The underlying HTTP client.
        /// </summary>
        protected HttpClient Http => Client.Http;

        /// <summary>
        ///     Get a single resource, returning <c>null</c> if it does not exist.
        /// </summary>
        /// <typeparam name="TResource">
        ///     The type of resource to retrieve.
        /// </typeparam>
        /// <param name="request">
        ///     An <see cref="HttpRequest"/> representing the resource to retrieve.
        /// </param>
        /// <param name="cancellationToken">
        ///     An optional <see cref="CancellationToken"/> that can be used to cancel the request.
        /// </param>
        /// <returns>
        ///     A <typeparamref name="TResource"/> representing the current state for the resource, or <c>null</c> if no resource was found with the specified name and namespace.
        /// </returns>
        protected async Task<TResource> GetSingleResource<TResource>(HttpRequest request, CancellationToken cancellationToken = default(CancellationToken))
            where TResource : class
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            
            using (HttpResponseMessage responseMessage = await Http.GetAsync(request, cancellationToken))
            {
                if (responseMessage.IsSuccessStatusCode)
                    return await responseMessage.ReadContentAsAsync<TResource>();

                // Ensure that HttpStatusCode.NotFound actually refers to the ReplicationController.
                UnversionedStatus status = await responseMessage.ReadContentAsAsync<UnversionedStatus, UnversionedStatus>(HttpStatusCode.NotFound);
                if (status.Reason != "NotFound")
                    throw new HttpRequestException<UnversionedStatus>(responseMessage.StatusCode, status);

                return null;
            }
        }
    }
}