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
    ///     A client for the Kubernetes Jobs (v1) API.
    /// </summary>
    public class JobClientV1
        : KubeResourceClient
    {
        /// <summary>
        ///     Create a new <see cref="JobClientV1"/>.
        /// </summary>
        /// <param name="client">
        ///     The Kubernetes API client.
        /// </param>
        public JobClientV1(KubeApiClient client)
            : base(client)
        {
        }

        /// <summary>
        ///     Get all Jobs in the specified namespace, optionally matching a label selector.
        /// </summary>
        /// <param name="kubeNamespace">
        ///     The target Kubernetes namespace (defaults to <see cref="KubeApiClient.DefaultNamespace"/>).
        /// </param>
        /// <param name="labelSelector">
        ///     An optional Kubernetes label selector expression used to filter the Jobs.
        /// </param>
        /// <param name="cancellationToken">
        ///     An optional <see cref="CancellationToken"/> that can be used to cancel the request.
        /// </param>
        /// <returns>
        ///     The Jobs, as a list of <see cref="V1Job"/>s.
        /// </returns>
        public async Task<List<V1Job>> List(string labelSelector = null, string kubeNamespace = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            V1JobList matchingJobs =
                await Http.GetAsync(
                    Requests.Collection.WithTemplateParameters(new
                    {
                        Namespace = kubeNamespace ?? Client.DefaultNamespace,
                        LabelSelector = labelSelector
                    }),
                    cancellationToken: cancellationToken
                )
                .ReadContentAsAsync<V1JobList, UnversionedStatus>();

            return matchingJobs.Items;
        }

        /// <summary>
        ///     Get the Job with the specified name.
        /// </summary>
        /// <param name="name">
        ///     The name of the Job to retrieve.
        /// </param>
        /// <param name="kubeNamespace">
        ///     The target Kubernetes namespace (defaults to <see cref="KubeApiClient.DefaultNamespace"/>).
        /// </param>
        /// <param name="cancellationToken">
        ///     An optional <see cref="CancellationToken"/> that can be used to cancel the request.
        /// </param>
        /// <returns>
        ///     A <see cref="V1Job"/> representing the current state for the Job, or <c>null</c> if no Job was found with the specified name and namespace.
        /// </returns>
        public async Task<V1Job> Get(string name, string kubeNamespace = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (String.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Argument cannot be null, empty, or entirely composed of whitespace: 'name'.", nameof(name));
            
            return await GetSingleResource<V1Job>(
                Requests.ByName.WithTemplateParameters(new
                {
                    Name = name,
                    Namespace = kubeNamespace ?? Client.DefaultNamespace
                }),
                cancellationToken: cancellationToken
            );
        }

        /// <summary>
        ///     Request creation of a <see cref="Job"/>.
        /// </summary>
        /// <param name="newJob">
        ///     A <see cref="V1Job"/> representing the Job to create.
        /// </param>
        /// <param name="cancellationToken">
        ///     An optional <see cref="CancellationToken"/> that can be used to cancel the request.
        /// </param>
        /// <returns>
        ///     A <see cref="V1Job"/> representing the current state for the newly-created Job.
        /// </returns>
        public async Task<V1Job> Create(V1Job newJob, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (newJob == null)
                throw new ArgumentNullException(nameof(newJob));
            
            return await Http
                .PostAsJsonAsync(
                    Requests.Collection.WithTemplateParameters(new
                    {
                        Namespace = newJob?.Metadata?.Namespace ?? Client.DefaultNamespace
                    }),
                    postBody: newJob,
                    cancellationToken: cancellationToken
                )
                .ReadContentAsAsync<V1Job, UnversionedStatus>();
        }

        /// <summary>
        ///     Request deletion of the specified Job.
        /// </summary>
        /// <param name="name">
        ///     The name of the Job to delete.
        /// </param>
        /// <param name="kubeNamespace">
        ///     The Kubernetes namespace containing the Job to delete.
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
        public async Task<UnversionedStatus> Delete(string name, string kubeNamespace = null, DeletePropagationPolicy propagationPolicy = DeletePropagationPolicy.Background, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await Http
                .DeleteAsJsonAsync(
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
        ///     Request templates for the Job (v1) API.
        /// </summary>
        static class Requests
        {
            /// <summary>
            ///     A collection-level Job (v1) request.
            /// </summary>
            public static readonly HttpRequest Collection = HttpRequest.Factory.Json("apis/batch/v1/namespaces/{Namespace}/jobs?labelSelector={LabelSelector?}", SerializerSettings);

            /// <summary>
            ///     A get-by-name Job (v1) request.
            /// </summary>
            public static readonly HttpRequest ByName = HttpRequest.Factory.Json("apis/batch/v1/namespaces/{Namespace}/jobs/{Name}", SerializerSettings);
        }
    }
}
