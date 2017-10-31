using HTTPlease;
using KubeNET.Swagger.Model;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using System.Net;

namespace DaasDemo.KubeClient.Clients
{
    using Models;

    /// <summary>
    ///     A client for the Kubernetes ReplicationControllers (v1) API.
    /// </summary>
    public class ReplicationControllerClientV1
        : KubeResourceClient
    {
        /// <summary>
        ///     Create a new <see cref="ReplicationControllerClientV1"/>.
        /// </summary>
        /// <param name="client">
        ///     The Kubernetes API client.
        /// </param>
        public ReplicationControllerClientV1(KubeApiClient client)
            : base(client)
        {
        }

        /// <summary>
        ///     Get all ReplicationControllers in the specified namespace, optionally matching a label selector.
        /// </summary>
        /// <param name="kubeNamespace">
        ///     The target Kubernetes namespace.
        /// </param>
        /// <param name="labelSelector">
        ///     An optional Kubernetes label selector expression used to filter the ReplicationControllers.
        /// </param>
        /// <param name="cancellationToken">
        ///     An optional <see cref="CancellationToken"/> that can be used to cancel the request.
        /// </param>
        /// <returns>
        ///     The ReplicationControllers, as a list of <see cref="V1ReplicationController"/>s.
        /// </returns>
        public async Task<List<V1ReplicationController>> List(string kubeNamespace, string labelSelector = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (String.IsNullOrWhiteSpace(kubeNamespace))
                throw new ArgumentException("Argument cannot be null, empty, or entirely composed of whitespace: 'kubeNamespace'.", nameof(kubeNamespace));

            V1ReplicationControllerList matchingControllers =
                await Http.GetAsync(
                    Requests.Collection.WithTemplateParameters(new
                    {
                        Namespace = kubeNamespace,
                        LabelSelector = labelSelector
                    }),
                    cancellationToken: cancellationToken
                )
                .ReadContentAsAsync<V1ReplicationControllerList, UnversionedStatus>();

            return matchingControllers.Items;
        }

        /// <summary>
        ///     Get the ReplicationController with the specified name.
        /// </summary>
        /// <param name="name">
        ///     The name of the ReplicationController to retrieve.
        /// </param>
        /// <param name="kubeNamespace">
        ///     The Kubernetes namespace containing the ReplicationController to retrieve.
        /// </param>
        /// <param name="cancellationToken">
        ///     An optional <see cref="CancellationToken"/> that can be used to cancel the request.
        /// </param>
        /// <returns>
        ///     A <see cref="V1ReplicationController"/> representing the current state for the ReplicationController, or <c>null</c> if no ReplicationController was found with the specified name and namespace.
        /// </returns>
        public async Task<V1ReplicationController> GetByName(string name, string kubeNamespace, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (String.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Argument cannot be null, empty, or entirely composed of whitespace: 'name'.", nameof(name));
            
            if (String.IsNullOrWhiteSpace(kubeNamespace))
                throw new ArgumentException("Argument cannot be null, empty, or entirely composed of whitespace: 'kubeNamespace'.", nameof(kubeNamespace));

            return await GetSingleResource<V1ReplicationController>(
                Requests.Collection.WithTemplateParameters(new
                {
                    Name = name,
                    Namespace = kubeNamespace
                }),
                cancellationToken: cancellationToken
            );
        }

        /// <summary>
        ///     Request creation of a <see cref="ReplicationController"/>.
        /// </summary>
        /// <param name="newController">
        ///     A <see cref="V1ReplicationController"/> representing the ReplicationController to create.
        /// </param>
        /// <param name="cancellationToken">
        ///     An optional <see cref="CancellationToken"/> that can be used to cancel the request.
        /// </param>
        /// <returns>
        ///     A <see cref="V1ReplicationController"/> representing the current state for the newly-created ReplicationController.
        /// </returns>
        public async Task<V1ReplicationController> Create(V1ReplicationController newController, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (newController == null)
                throw new ArgumentNullException(nameof(newController));
            
            return await Http
                .PostAsJsonAsync(
                    Requests.Collection.WithTemplateParameters(new
                    {
                        Namespace = newController?.Metadata?.Namespace ?? KubeApiClient.DefaultNamespace
                    }),
                    postBody: newController,
                    cancellationToken: cancellationToken
                )
                .ReadContentAsAsync<V1ReplicationController, UnversionedStatus>();
        }

        /// <summary>
        ///     Request deletion of the specified ReplicationController.
        /// </summary>
        /// <param name="name">
        ///     The name of the ReplicationController to delete.
        /// </param>
        /// <param name="kubeNamespace">
        ///     The Kubernetes namespace containing the ReplicationController to delete.
        /// </param>
        /// <param name="propagationPolicy">
        ///     A <see cref="DeletePropagationPolicy"/> indicating how child resources should be deleted (if at all).
        /// </param>
        /// <param name="cancellationToken">
        ///     An optional <see cref="CancellationToken"/> that can be used to cancel the request.
        /// </param>
        /// <returns>
        ///     An <see cref="UnversionedStatus"/> indicating the result of the request.
        /// </returns>
        public async Task<UnversionedStatus> Delete(string name, string kubeNamespace, DeletePropagationPolicy propagationPolicy = DeletePropagationPolicy.Background, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await Http
                .DeleteAsJsonAsync(
                    Requests.ByName.WithTemplateParameters(new
                    {
                        Name = name,
                        Namespace = kubeNamespace
                    }),
                    deleteBody: new
                    {
                        apiVersion = "v1",
                        kind = "DeleteOptions",
                        propagationPolicy = propagationPolicy
                    },
                    cancellationToken: cancellationToken
                )
                .ReadContentAsAsync<UnversionedStatus, UnversionedStatus>(HttpStatusCode.OK, HttpStatusCode.NotFound);
        }

        /// <summary>
        ///     Request templates for the ReplicationController (v1) API.
        /// </summary>
        static class Requests
        {
            /// <summary>
            ///     A collection-level ReplicationController (v1) request.
            /// </summary>
            public static readonly HttpRequest Collection = HttpRequest.Factory.Json("api/v1/namespaces/{Namespace}/replicationcontrollers?labelSelector={LabelSelector?}");

            /// <summary>
            ///     A get-by-name ReplicationController (v1) request.
            /// </summary>
            public static readonly HttpRequest ByName = HttpRequest.Factory.Json("api/v1/namespaces/{Namespace}/replicationcontrollers/{Name}");
        }
    }
}