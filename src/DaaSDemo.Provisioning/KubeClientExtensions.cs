using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DaaSDemo.Provisioning
{
    using KubeClient;
    using KubeClient.Models;
    using Models.Data;

    /// <summary>
    ///     Extension methods for <see cref="KubeApiClient"/>.
    /// </summary>
    public static class KubeClientExtensions
    {
        /// <summary>
        ///     Get the public TCP port number on which the database server is accessible.
        /// </summary>
        /// <param name="server">
        ///     A <see cref="DatabaseServer"/> describing the server.
        /// </param>
        /// <param name="kubeNamespace">
        ///     An optional target Kubernetes namespace.
        /// </param>
        /// <returns>
        ///     The port, or <c>null</c> if the externally-facing service for the server cannot be found.
        /// </returns>
        public static async Task<int?> GetServerPublicPort(this KubeApiClient client, DatabaseServer server, string kubeNamespace = null)
        {
            if (client == null)
                throw new ArgumentNullException(nameof(client));

            if (server == null)
                throw new ArgumentNullException(nameof(server));

            List<ServiceV1> matchingServices = await client.ServicesV1().List(
                labelSelector: $"cloud.dimensiondata.daas.server-id = {server.Id},cloud.dimensiondata.daas.service-type = external",
                kubeNamespace: kubeNamespace
            );
            if (matchingServices.Count == 0)
                return null;

            ServiceV1 externalService = matchingServices[matchingServices.Count - 1];

            return externalService.Spec.Ports[0].NodePort;
        }
    }
}
