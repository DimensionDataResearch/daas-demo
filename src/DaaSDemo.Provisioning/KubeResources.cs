using KubeNET.Swagger.Model;
using System;
using System.Collections.Generic;

namespace DaaSDemo.Provisioning
{
    using Data.Models;
    using KubeClient.Models;

    /// <summary>
    ///     Factory methods for common Kubernetes resources.
    /// </summary>
    public static class KubeResources
    {
        /// <summary>
        ///     Get the base resource name for the specified server.
        /// </summary>
        /// <param name="server">
        ///     A <see cref="DatabaseServer"/> representing the target server
        /// </param>
        /// <returns>
        ///     The base resource name.
        /// </returns>
        public static string GetBaseName(DatabaseServer server) => $"sql-server-{server.Id}";

        /// <summary>
        ///     Create a new <see cref="V1ReplicationController"/> for the specified database server.
        /// </summary>
        /// <param name="server">
        ///     A <see cref="DatabaseServer"/> representing the target server.
        /// </param>
        /// <param name="dataVolumeClaimName">
        ///     The name of the Kubernetes VolumeClaim where the data will be stored.
        /// </param>
        /// <returns>
        ///     The configured <see cref="V1ReplicationController"/>.
        /// </returns>
        public static V1ReplicationController ReplicationController(DatabaseServer server, string dataVolumeClaimName)
        {
            if (server == null)
                throw new ArgumentNullException(nameof(server));

            string baseName = GetBaseName(server);
            
            return ReplicationController(
                name: baseName,
                spec: KubeSpecs.ReplicationController(server, dataVolumeClaimName)
            );
        }
        
        /// <summary>
        ///     Create a new <see cref="V1ReplicationController"/>.
        /// </summary>
        /// <param name="name">
        ///     The replication controller name.
        /// </param>
        /// <param name="spec">
        ///     A <see cref="V1ReplicationControllerSpec"/> representing the controller specification.
        /// </param>
        /// <param name="labels">
        ///     An optional <see cref="Dictionary{TKey, TValue}"/> containing labels to apply to the replication controller.
        /// </param>
        /// <param name="annotations">
        ///     An optional <see cref="Dictionary{TKey, TValue}"/> containing annotations to apply to the replication controller.
        /// </param>
        /// <returns>
        ///     The configured <see cref="V1ReplicationController"/>.
        /// </returns>
        public static V1ReplicationController ReplicationController(string name, V1ReplicationControllerSpec spec, Dictionary<string, string> labels = null, Dictionary<string, string> annotations = null)
        {
            if (String.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Argument cannot be null, empty, or entirely composed of whitespace: 'name'.", nameof(name));
            
            if (spec == null)
                throw new ArgumentNullException(nameof(spec));

            return new V1ReplicationController
            {
                ApiVersion = "v1",
                Kind = "ReplicationController",
                Metadata = new V1ObjectMeta
                {
                    Name = name,
                    Labels = labels,
                    Annotations = annotations
                },
                Spec = spec
            };
        }

        /// <summary>
        ///     Create a new <see cref="V1Service"/> for the specified database server.
        /// </summary>
        /// <param name="server">
        ///     A <see cref="DatabaseServer"/> representing the target server.
        /// </param>
        /// <returns>
        ///     The configured <see cref="V1Service"/>.
        /// </returns>
        public static V1Service Service(DatabaseServer server)
        {
            if (server == null)
                throw new ArgumentNullException(nameof(server));

            string baseName = GetBaseName(server);
            
            return Service(
                name: $"{baseName}-service",
                spec: KubeSpecs.Service(server),
                labels: new Dictionary<string, string>
                {
                    ["k8s-app"] = baseName,
                    ["cloud.dimensiondata.daas.server-id"] = server.Id.ToString()
                }
            );
        }

        /// <summary>
        ///     Create a new <see cref="V1Service"/>.
        /// </summary>
        /// <param name="name">
        ///     The service name.
        /// </param>
        /// <param name="spec">
        ///     A <see cref="V1ServiceSpec"/> representing the service specification.
        /// </param>
        /// <param name="labels">
        ///     An optional <see cref="Dictionary{TKey, TValue}"/> containing labels to apply to the service.
        /// </param>
        /// <param name="annotations">
        ///     An optional <see cref="Dictionary{TKey, TValue}"/> containing annotations to apply to the service.
        /// </param>
        /// <returns>
        ///     The configured <see cref="V1Service"/>.
        /// </returns>
        public static V1Service Service(string name, V1ServiceSpec spec, Dictionary<string, string> labels = null, Dictionary<string, string> annotations = null)
        {
            if (String.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Argument cannot be null, empty, or entirely composed of whitespace: 'name'.", nameof(name));
            
            if (spec == null)
                throw new ArgumentNullException(nameof(spec));

            return new V1Service
            {
                ApiVersion = "v1",
                Kind = "Service",
                Metadata = new V1ObjectMeta
                {
                    Name = name,
                    Labels = labels,
                    Annotations = annotations
                },
                Spec = spec
            };
        }

        /// <summary>
        ///     Create a new <see cref="V1Beta1VoyagerIngress"/> for the specified database server.
        /// </summary>
        /// <param name="server">
        ///     A <see cref="DatabaseServer"/> representing the target server.
        /// </param>
        /// <returns>
        ///     The configured <see cref="V1Beta1VoyagerIngress"/>.
        /// </returns>
        public static V1Beta1VoyagerIngress Ingress(DatabaseServer server)
        {
            if (server == null)
                throw new ArgumentNullException(nameof(server));

            string baseName = GetBaseName(server);

            return Ingress(
                name: $"{baseName}-ingress",
                spec: KubeSpecs.Ingress(server),
                labels: new Dictionary<string, string>
                {
                    ["cloud.dimensiondata.daas.server-id"] = server.Id.ToString()
                },
                annotations: new Dictionary<string, string>
                {
                    ["ingress.appscode.com/type"] = "HostPort",
                    ["kubernetes.io/ingress.class"] = "voyager"
                }
            );
        }

        /// <summary>
        ///     Create a new <see cref="V1Beta1VoyagerIngress"/>.
        /// </summary>
        /// <param name="name">
        ///     The ingress name.
        /// </param>
        /// <param name="spec">
        ///     A <see cref="V1Beta1VoyagerIngressSpec"/> representing the ingress specification.
        /// </param>
        /// <param name="labels">
        ///     An optional <see cref="Dictionary{TKey, TValue}"/> containing labels to apply to the ingress.
        /// </param>
        /// <param name="annotations">
        ///     An optional <see cref="Dictionary{TKey, TValue}"/> containing annotations to apply to the ingress.
        /// </param>
        /// <returns>
        ///     The configured <see cref="V1Beta1VoyagerIngress"/>.
        /// </returns>
        public static V1Beta1VoyagerIngress Ingress(string name, V1Beta1VoyagerIngressSpec spec, Dictionary<string, string> labels = null, Dictionary<string, string> annotations = null)
        {
            if (String.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Argument cannot be null, empty, or entirely composed of whitespace: 'name'.", nameof(name));
            
            if (spec == null)
                throw new ArgumentNullException(nameof(spec));

            return new V1Beta1VoyagerIngress
            {
                ApiVersion = "voyager.appscode.com/v1beta1",
                Kind = "Ingress",
                Metadata = new V1ObjectMeta
                {
                    Name = name,
                    Labels = labels,
                    Annotations = annotations
                },
                Spec = spec
            };
        }
    }
}