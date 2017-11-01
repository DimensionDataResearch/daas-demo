using HTTPlease;
using KubeNET.Swagger.Model;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using System.Net;

namespace DaaSDemo.KubeClient.Clients
{
    using Models;

    /// <summary>
    ///     A client for the Kubernetes Services (v1) API.
    /// </summary>
    public class ServiceClientV1
        : KubeResourceClient
    {
        /// <summary>
        ///     Create a new <see cref="ServiceClientV1"/>.
        /// </summary>
        /// <param name="client">
        ///     The Kubernetes API client.
        /// </param>
        public ServiceClientV1(KubeApiClient client)
            : base(client)
        {
        }

        /// <summary>
        ///     Get all Services in the specified namespace, optionally matching a label selector.
        /// </summary>
        /// <param name="labelSelector">
        ///     An optional Kubernetes label selector expression used to filter the Services.
        /// </param>
        /// <param name="kubeNamespace">
        ///     The target Kubernetes namespace (defaults to <see cref="KubeApiClient.DefaultNamespace"/>).
        /// </param>
        /// <param name="cancellationToken">
        ///     An optional <see cref="CancellationToken"/> that can be used to cancel the request.
        /// </param>
        /// <returns>
        ///     The Services, as a list of <see cref="V1Service"/>s.
        /// </returns>
        public async Task<List<V1Service>> List(string labelSelector = null, string kubeNamespace = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            V1ServiceList matchingServices =
                await Http.GetAsync(
                    Requests.Collection.WithTemplateParameters(new
                    {
                        Namespace = kubeNamespace ?? Client.DefaultNamespace,
                        LabelSelector = labelSelector
                    }),
                    cancellationToken: cancellationToken
                )
                .ReadContentAsAsync<V1ServiceList, UnversionedStatus>();

            return matchingServices.Items;
        }

        /// <summary>
        ///     Get the Service with the specified name.
        /// </summary>
        /// <param name="name">
        ///     The name of the Service to retrieve.
        /// </param>
        /// <param name="kubeNamespace">
        ///     The target Kubernetes namespace (defaults to <see cref="KubeApiClient.DefaultNamespace"/>).
        /// </param>
        /// <param name="cancellationToken">
        ///     An optional <see cref="CancellationToken"/> that can be used to cancel the request.
        /// </param>
        /// <returns>
        ///     A <see cref="V1Service"/> representing the current state for the Service, or <c>null</c> if no Service was found with the specified name and namespace.
        /// </returns>
        public async Task<V1Service> GetByName(string name, string kubeNamespace = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (String.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Argument cannot be null, empty, or entirely composed of whitespace: 'name'.", nameof(name));
            
            return await GetSingleResource<V1Service>(
                Requests.ByName.WithTemplateParameters(new
                {
                    Name = name,
                    Namespace = kubeNamespace ?? Client.DefaultNamespace
                }),
                cancellationToken: cancellationToken
            );
        }

        /// <summary>
        ///     Request creation of a <see cref="Service"/>.
        /// </summary>
        /// <param name="newService">
        ///     A <see cref="V1Service"/> representing the Service to create.
        /// </param>
        /// <param name="cancellationToken">
        ///     An optional <see cref="CancellationToken"/> that can be used to cancel the request.
        /// </param>
        /// <returns>
        ///     A <see cref="V1Service"/> representing the current state for the newly-created Service.
        /// </returns>
        public async Task<V1Service> Create(V1Service newService, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (newService == null)
                throw new ArgumentNullException(nameof(newService));
            
            return await Http
                .PostAsJsonAsync(
                    Requests.Collection.WithTemplateParameters(new
                    {
                        Namespace = newService?.Metadata?.Namespace ?? Client.DefaultNamespace
                    }),
                    postBody: newService,
                    cancellationToken: cancellationToken
                )
                .ReadContentAsAsync<V1Service, UnversionedStatus>();
        }

        /// <summary>
        ///     Request deletion of the specified Service.
        /// </summary>
        /// <param name="name">
        ///     The name of the Service to delete.
        /// </param>
        /// <param name="kubeNamespace">
        ///     The target Kubernetes namespace (defaults to <see cref="KubeApiClient.DefaultNamespace"/>).
        /// </param>
        /// <param name="cancellationToken">
        ///     An optional <see cref="CancellationToken"/> that can be used to cancel the request.
        /// </param>
        /// <returns>
        ///     An <see cref="UnversionedStatus"/> indicating the result of the request.
        /// </returns>
        public async Task<UnversionedStatus> Delete(string name, string kubeNamespace = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await Http
                .DeleteAsync(
                    Requests.ByName.WithTemplateParameters(new
                    {
                        Name = name,
                        Namespace = kubeNamespace ?? Client.DefaultNamespace
                    }),
                    cancellationToken: cancellationToken
                )
                .ReadContentAsAsync<UnversionedStatus, UnversionedStatus>(HttpStatusCode.OK, HttpStatusCode.NotFound);
        }

        /// <summary>
        ///     Request templates for the Service (v1) API.
        /// </summary>
        static class Requests
        {
            /// <summary>
            ///     A collection-level Service (v1) request.
            /// </summary>
            public static readonly HttpRequest Collection = HttpRequest.Factory.Json("api/v1/namespaces/{Namespace}/services?labelSelector={LabelSelector?}", SerializerSettings);

            /// <summary>
            ///     A get-by-name Service (v1) request.
            /// </summary>
            public static readonly HttpRequest ByName = HttpRequest.Factory.Json("api/v1/namespaces/{Namespace}/services/{Name}", SerializerSettings);
        }
    }
}