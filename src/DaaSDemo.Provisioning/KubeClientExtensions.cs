using KubeNET.Swagger.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DaaSDemo.Provisioning
{
    using Data.Models;
    using KubeClient;

    /// <summary>
    ///     Extension methods for <see cref="KubeApiClient"/>.
    /// </summary>
    public static class KubeClientExtensions
    {
        /// <summary>
        ///     Get the (node) IP and port on which the database server is accessible.
        /// </summary>
        /// <param name="server">
        ///     A <see cref="DatabaseServer"/> describing the server.
        /// </param>
        /// <returns>
        ///     The host and port, or <c>null</c> and <c>null</c> if the ingress for the server cannot be found.
        /// </returns>
        public static async Task<(string host, int? hostPort)> GetServerIngressEndPoint(this KubeApiClient client, DatabaseServer server)
        {
            if (client == null)
                throw new ArgumentNullException(nameof(client));

            if (server == null)
                throw new ArgumentNullException(nameof(server));

            List<V1Service> matchingServices = await client.ServicesV1.List(
                labelSelector: $"cloud.dimensiondata.daas.server-id = {server.Id},cloud.dimensiondata.daas.service-type = external"
            );
            if (matchingServices.Count == 0)
                return (null, null);

            V1Service externalService = matchingServices[matchingServices.Count - 1];

            return (
                host: "kr-cluster.tintoy.io", // TODO: Make this configurable.
                hostPort: externalService.Spec.Ports[0].NodePort
            );
        }
    }
}
