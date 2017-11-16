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
    ///     A client for the Kubernetes Deployments (v1beta2) API.
    /// </summary>
    public class DeploymentClientV1Beta1
        : KubeResourceClient
    {
        /// <summary>
        ///     Create a new <see cref="DeploymentClientV1Beta1"/>.
        /// </summary>
        /// <param name="client">
        ///     The Kubernetes API client.
        /// </param>
        public DeploymentClientV1Beta1(KubeApiClient client)
            : base(client)
        {
        }

        /// <summary>
        ///     Get all Deployments in the specified namespace, optionally matching a label selector.
        /// </summary>
        /// <param name="labelSelector">
        ///     An optional Kubernetes label selector expression used to filter the Deployments.
        /// </param>
        /// <param name="kubeNamespace">
        ///     The target Kubernetes namespace (defaults to <see cref="KubeApiClient.DefaultNamespace"/>).
        /// </param>
        /// <param name="cancellationToken">
        ///     An optional <see cref="CancellationToken"/> that can be used to cancel the request.
        /// </param>
        /// <returns>
        ///     The Deployments, as a list of <see cref="V1beta1Deployment"/>s.
        /// </returns>
        public async Task<List<V1beta1Deployment>> List(string labelSelector = null, string kubeNamespace = null, CancellationToken cancellationToken = default)
        {
            V1beta1DeploymentList matchingControllers =
                await Http.GetAsync(
                    Requests.Collection.WithTemplateParameters(new
                    {
                        Namespace = kubeNamespace ?? Client.DefaultNamespace,
                        LabelSelector = labelSelector
                    }),
                    cancellationToken: cancellationToken
                )
                .ReadContentAsAsync<V1beta1DeploymentList, UnversionedStatus>();

            return matchingControllers.Items;
        }

        /// <summary>
        ///     Watch for events relating to Deployments.
        /// </summary>
        /// <param name="labelSelector">
        ///     An optional Kubernetes label selector expression used to filter the Deployments.
        /// </param>
        /// <param name="kubeNamespace">
        ///     The target Kubernetes namespace (defaults to <see cref="KubeApiClient.DefaultNamespace"/>).
        /// </param>
        /// <returns>
        ///     An <see cref="IObservable{T}"/> representing the event stream.
        /// </returns>
        public IObservable<ResourceEventV1<V1beta1Deployment>> WatchAll(string labelSelector = null, string kubeNamespace = null)
        {
            return ObserveEvents<V1beta1Deployment>(
                Requests.Collection.WithTemplateParameters(new
                {
                    Namespace = kubeNamespace ?? Client.DefaultNamespace,
                    LabelSelector = labelSelector,
                    Watch = true
                })
            );
        }

        /// <summary>
        ///     Get the Deployment with the specified name.
        /// </summary>
        /// <param name="name">
        ///     The name of the Deployment to retrieve.
        /// </param>
        /// <param name="kubeNamespace">
        ///     The target Kubernetes namespace (defaults to <see cref="KubeApiClient.DefaultNamespace"/>).
        /// </param>
        /// <param name="cancellationToken">
        ///     An optional <see cref="CancellationToken"/> that can be used to cancel the request.
        /// </param>
        /// <returns>
        ///     A <see cref="V1beta1Deployment"/> representing the current state for the Deployment, or <c>null</c> if no Deployment was found with the specified name and namespace.
        /// </returns>
        public async Task<V1beta1Deployment> Get(string name, string kubeNamespace = null, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Argument cannot be null, empty, or entirely composed of whitespace: 'name'.", nameof(name));

            return await GetSingleResource<V1beta1Deployment>(
                Requests.ByName.WithTemplateParameters(new
                {
                    Name = name,
                    Namespace = kubeNamespace ?? Client.DefaultNamespace
                }),
                cancellationToken: cancellationToken
            );
        }

        /// <summary>
        ///     Request creation of a <see cref="Deployment"/>.
        /// </summary>
        /// <param name="newController">
        ///     A <see cref="V1beta1Deployment"/> representing the Deployment to create.
        /// </param>
        /// <param name="cancellationToken">
        ///     An optional <see cref="CancellationToken"/> that can be used to cancel the request.
        /// </param>
        /// <returns>
        ///     A <see cref="V1beta1Deployment"/> representing the current state for the newly-created Deployment.
        /// </returns>
        public async Task<V1beta1Deployment> Create(V1beta1Deployment newController, CancellationToken cancellationToken = default)
        {
            if (newController == null)
                throw new ArgumentNullException(nameof(newController));
            
            return await Http
                .PostAsJsonAsync(
                    Requests.Collection.WithTemplateParameters(new
                    {
                        Namespace = newController?.Metadata?.Namespace ?? Client.DefaultNamespace
                    }),
                    postBody: newController,
                    cancellationToken: cancellationToken
                )
                .ReadContentAsAsync<V1beta1Deployment, UnversionedStatus>();
        }

        /// <summary>
        ///     Request deletion of the specified Deployment.
        /// </summary>
        /// <param name="name">
        ///     The name of the Deployment to delete.
        /// </param>
        /// <param name="kubeNamespace">
        ///     The target Kubernetes namespace (defaults to <see cref="KubeApiClient.DefaultNamespace"/>).
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
        public async Task<UnversionedStatus> Delete(string name, string kubeNamespace = null, DeletePropagationPolicy propagationPolicy = DeletePropagationPolicy.Background, CancellationToken cancellationToken = default)
        {
            return
                await Http.DeleteAsJsonAsync(
                    Requests.ByName.WithTemplateParameters(new
                    {
                        Name = name,
                        Namespace = kubeNamespace ?? Client.DefaultNamespace
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
        ///     Request templates for the Deployment (v1beta2) API.
        /// </summary>
        static class Requests
        {
            /// <summary>
            ///     A collection-level Deployment (v1beta2) request.
            /// </summary>
            public static readonly HttpRequest Collection = HttpRequest.Factory.Json("apis/apps/v1beta1/namespaces/{Namespace}/deployments?labelSelector={LabelSelector?}&watch={Watch?}", SerializerSettings);

            /// <summary>
            ///     A get-by-name Deployment (v1beta2) request.
            /// </summary>
            public static readonly HttpRequest ByName = HttpRequest.Factory.Json("apis/apps/v1beta1/namespaces/{Namespace}/deployments/{Name}", SerializerSettings);
        }
    }
}
