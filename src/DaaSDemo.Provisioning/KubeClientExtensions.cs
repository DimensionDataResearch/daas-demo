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
        ///     The IP and port, or <c>null</c> and <c>null</c> if the ingress for the server cannot be found.
        /// </returns>
        public static async Task<(string hostIP, int? hostPort)> GetServerIngressEndPoint(this KubeApiClient client, DatabaseServer server)
        {
            if (client == null)
                throw new ArgumentNullException(nameof(client));

            if (server == null)
                throw new ArgumentNullException(nameof(server));

            string baseName = KubeResources.GetBaseName(server);
            string ingressResourceName = $"{baseName}-ingress";

            // Find the Pod that implements the ingress (the origin-name label will point back to the ingress resource).
            List<V1Pod> matchingPods = await client.PodsV1.List(
                labelSelector: $"origin-name = {ingressResourceName}"
            );

            V1Pod ingressPod = matchingPods.FirstOrDefault(item => item.Status.Phase == "Running");
            if (ingressPod == null)
                return (hostIP: null, hostPort: null);

            return (
                hostIP: ingressPod.Status.HostIP,
                hostPort: ingressPod.Spec.Containers[0].Ports[0].HostPort
            );
        }
    }
}