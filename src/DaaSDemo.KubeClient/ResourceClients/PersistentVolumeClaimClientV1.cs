using HTTPlease;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using System.Net;

namespace DaaSDemo.KubeClient.ResourceClients
{
    using Models;

    /// <summary>
    ///     A client for the Kubernetes PersistentVolumeClaims (v1) API.
    /// </summary>
    public class PersistentVolumeClaimClientV1
        : KubeResourceClient
    {
        /// <summary>
        ///     Create a new <see cref="PersistentVolumeClaimClientV1"/>.
        /// </summary>
        /// <param name="client">
        ///     The Kubernetes API client.
        /// </param>
        public PersistentVolumeClaimClientV1(KubeApiClient client)
            : base(client)
        {
        }

        /// <summary>
        ///     Get all PersistentVolumeClaims in the specified namespace, optionally matching a label selector.
        /// </summary>
        /// <param name="labelSelector">
        ///     An optional Kubernetes label selector expression used to filter the PersistentVolumeClaims.
        /// </param>
        /// <param name="kubeNamespace">
        ///     The target Kubernetes namespace (defaults to <see cref="KubeApiClient.DefaultNamespace"/>).
        /// </param>
        /// <param name="cancellationToken">
        ///     An optional <see cref="CancellationToken"/> that can be used to cancel the request.
        /// </param>
        /// <returns>
        ///     The PersistentVolumeClaims, as a list of <see cref="PersistentVolumeClaimV1"/>s.
        /// </returns>
        public async Task<List<PersistentVolumeClaimV1>> List(string labelSelector = null, string kubeNamespace = null, CancellationToken cancellationToken = default)
        {
            PersistentVolumeClaimListV1 matchingPersistentVolumeClaims =
                await Http.GetAsync(
                    Requests.Collection.WithTemplateParameters(new
                    {
                        Namespace = kubeNamespace ?? Client.DefaultNamespace,
                        LabelSelector = labelSelector
                    }),
                    cancellationToken: cancellationToken
                )
                .ReadContentAsAsync<PersistentVolumeClaimListV1, StatusV1>();

            return matchingPersistentVolumeClaims.Items;
        }

        /// <summary>
        ///     Get the PersistentVolumeClaim with the specified name.
        /// </summary>
        /// <param name="name">
        ///     The name of the PersistentVolumeClaim to retrieve.
        /// </param>
        /// <param name="kubeNamespace">
        ///     The target Kubernetes namespace (defaults to <see cref="KubeApiClient.DefaultNamespace"/>).
        /// </param>
        /// <param name="cancellationToken">
        ///     An optional <see cref="CancellationToken"/> that can be used to cancel the request.
        /// </param>
        /// <returns>
        ///     A <see cref="PersistentVolumeClaimV1"/> representing the current state for the PersistentVolumeClaim, or <c>null</c> if no PersistentVolumeClaim was found with the specified name and namespace.
        /// </returns>
        public async Task<PersistentVolumeClaimV1> Get(string name, string kubeNamespace = null, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Argument cannot be null, empty, or entirely composed of whitespace: 'name'.", nameof(name));
            
            return await GetSingleResource<PersistentVolumeClaimV1>(
                Requests.ByName.WithTemplateParameters(new
                {
                    Name = name,
                    Namespace = kubeNamespace ?? Client.DefaultNamespace
                }),
                cancellationToken: cancellationToken
            );
        }

        /// <summary>
        ///     Request creation of a <see cref="PersistentVolumeClaim"/>.
        /// </summary>
        /// <param name="newPersistentVolumeClaim">
        ///     A <see cref="PersistentVolumeClaimV1"/> representing the PersistentVolumeClaim to create.
        /// </param>
        /// <param name="cancellationToken">
        ///     An optional <see cref="CancellationToken"/> that can be used to cancel the request.
        /// </param>
        /// <returns>
        ///     A <see cref="PersistentVolumeClaimV1"/> representing the current state for the newly-created PersistentVolumeClaim.
        /// </returns>
        public async Task<PersistentVolumeClaimV1> Create(PersistentVolumeClaimV1 newPersistentVolumeClaim, CancellationToken cancellationToken = default)
        {
            if (newPersistentVolumeClaim == null)
                throw new ArgumentNullException(nameof(newPersistentVolumeClaim));
            
            return await Http
                .PostAsJsonAsync(
                    Requests.Collection.WithTemplateParameters(new
                    {
                        Namespace = newPersistentVolumeClaim?.Metadata?.Namespace ?? Client.DefaultNamespace
                    }),
                    postBody: newPersistentVolumeClaim,
                    cancellationToken: cancellationToken
                )
                .ReadContentAsAsync<PersistentVolumeClaimV1, StatusV1>();
        }

        /// <summary>
        ///     Request deletion of the specified PersistentVolumeClaim.
        /// </summary>
        /// <param name="name">
        ///     The name of the PersistentVolumeClaim to delete.
        /// </param>
        /// <param name="kubeNamespace">
        ///     The target Kubernetes namespace (defaults to <see cref="KubeApiClient.DefaultNamespace"/>).
        /// </param>
        /// <param name="cancellationToken">
        ///     An optional <see cref="CancellationToken"/> that can be used to cancel the request.
        /// </param>
        /// <returns>
        ///     An <see cref="StatusV1"/> indicating the result of the request.
        /// </returns>
        public async Task<StatusV1> Delete(string name, string kubeNamespace = null, CancellationToken cancellationToken = default)
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
                .ReadContentAsAsync<StatusV1, StatusV1>(HttpStatusCode.OK, HttpStatusCode.NotFound);
        }

        /// <summary>
        ///     Request templates for the PersistentVolumeClaim (v1) API.
        /// </summary>
        static class Requests
        {
            /// <summary>
            ///     A collection-level PersistentVolumeClaim (v1) request.
            /// </summary>
            public static readonly HttpRequest Collection = HttpRequest.Factory.Json("api/v1/namespaces/{Namespace}/persistentvolumeclaims?labelSelector={LabelSelector?}", SerializerSettings);

            /// <summary>
            ///     A get-by-name PersistentVolumeClaim (v1) request.
            /// </summary>
            public static readonly HttpRequest ByName = HttpRequest.Factory.Json("api/v1/namespaces/{Namespace}/persistentvolumeclaims/{Name}", SerializerSettings);
        }
    }
}
