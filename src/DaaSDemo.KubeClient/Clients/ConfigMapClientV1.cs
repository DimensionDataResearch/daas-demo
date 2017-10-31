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
    ///     A client for the Kubernetes ConfigMaps (v1) API.
    /// </summary>
    public class ConfigMapClientV1
        : KubeResourceClient
    {
        /// <summary>
        ///     Create a new <see cref="ConfigMapClientV1"/>.
        /// </summary>
        /// <param name="client">
        ///     The Kubernetes API client.
        /// </param>
        public ConfigMapClientV1(KubeApiClient client)
            : base(client)
        {
        }

        /// <summary>
        ///     Get all ConfigMaps in the specified namespace, optionally matching a label selector.
        /// </summary>
        /// <param name="labelSelector">
        ///     An optional Kubernetes label selector expression used to filter the ConfigMaps.
        /// </param>
        /// <param name="kubeNamespace">
        ///     The target Kubernetes namespace (defaults to <see cref="KubeApiClient.DefaultNamespace"/>).
        /// </param>
        /// <param name="cancellationToken">
        ///     An optional <see cref="CancellationToken"/> that can be used to cancel the request.
        /// </param>
        /// <returns>
        ///     The ConfigMaps, as a list of <see cref="V1ConfigMap"/>s.
        /// </returns>
        public async Task<List<V1ConfigMap>> List(string labelSelector = null, string kubeNamespace = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            V1ConfigMapList matchingConfigMaps =
                await Http.GetAsync(
                    Requests.Collection.WithTemplateParameters(new
                    {
                        Namespace = kubeNamespace ?? Client.DefaultNamespace,
                        LabelSelector = labelSelector
                    }),
                    cancellationToken: cancellationToken
                )
                .ReadContentAsAsync<V1ConfigMapList, UnversionedStatus>();

            return matchingConfigMaps.Items;
        }

        /// <summary>
        ///     Get the ConfigMap with the specified name.
        /// </summary>
        /// <param name="name">
        ///     The name of the ConfigMap to retrieve.
        /// </param>
        /// <param name="kubeNamespace">
        ///     The target Kubernetes namespace (defaults to <see cref="KubeApiClient.DefaultNamespace"/>).
        /// </param>
        /// <param name="cancellationToken">
        ///     An optional <see cref="CancellationToken"/> that can be used to cancel the request.
        /// </param>
        /// <returns>
        ///     A <see cref="V1ConfigMap"/> representing the current state for the ConfigMap, or <c>null</c> if no ConfigMap was found with the specified name and namespace.
        /// </returns>
        public async Task<V1ConfigMap> GetByName(string name, string kubeNamespace = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (String.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Argument cannot be null, empty, or entirely composed of whitespace: 'name'.", nameof(name));
            
            return await GetSingleResource<V1ConfigMap>(
                Requests.Collection.WithTemplateParameters(new
                {
                    Name = name,
                    Namespace = kubeNamespace ?? Client.DefaultNamespace
                }),
                cancellationToken: cancellationToken
            );
        }

        /// <summary>
        ///     Request creation of a <see cref="ConfigMap"/>.
        /// </summary>
        /// <param name="newConfigMap">
        ///     A <see cref="V1ConfigMap"/> representing the ConfigMap to create.
        /// </param>
        /// <param name="cancellationToken">
        ///     An optional <see cref="CancellationToken"/> that can be used to cancel the request.
        /// </param>
        /// <returns>
        ///     A <see cref="V1ConfigMap"/> representing the current state for the newly-created ConfigMap.
        /// </returns>
        public async Task<V1ConfigMap> Create(V1ConfigMap newConfigMap, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (newConfigMap == null)
                throw new ArgumentNullException(nameof(newConfigMap));
            
            return await Http
                .PostAsJsonAsync(
                    Requests.Collection.WithTemplateParameters(new
                    {
                        Namespace = newConfigMap?.Metadata?.Namespace ?? Client.DefaultNamespace
                    }),
                    postBody: newConfigMap,
                    cancellationToken: cancellationToken
                )
                .ReadContentAsAsync<V1ConfigMap, UnversionedStatus>();
        }

        /// <summary>
        ///     Request deletion of the specified ConfigMap.
        /// </summary>
        /// <param name="name">
        ///     The name of the ConfigMap to delete.
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
        ///     Request templates for the ConfigMap (v1) API.
        /// </summary>
        static class Requests
        {
            /// <summary>
            ///     A collection-level ConfigMap (v1) request.
            /// </summary>
            public static readonly HttpRequest Collection = HttpRequest.Factory.Json("api/v1/namespaces/{Namespace}/configmaps?labelSelector={LabelSelector?}", SerializerSettings);

            /// <summary>
            ///     A get-by-name ConfigMap (v1) request.
            /// </summary>
            public static readonly HttpRequest ByName = HttpRequest.Factory.Json("api/v1/namespaces/{Namespace}/configmaps/{Name}", SerializerSettings);
        }
    }
}