using KubeNET.Swagger.Model;
using System;
using System.Collections.Generic;

namespace DaaSDemo.Provisioning
{
    using KubeClient.Models;
    using Messages;
    using Models.Data;

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
        ///     Create a new <see cref="V1beta1Deployment"/> for the specified database server.
        /// </summary>
        /// <param name="server">
        ///     A <see cref="DatabaseServer"/> representing the target server.
        /// </param>
        /// <param name="imageName">
        ///     The name (and tag) of the SQL Server for Linux image to use.
        /// </param>
        /// <param name="dataVolumeClaimName">
        ///     The name of the Kubernetes VolumeClaim where the data will be stored.
        /// </param>
        /// <returns>
        ///     The configured <see cref="V1beta1Deployment"/>.
        /// </returns>
        public static V1beta1Deployment Deployment(DatabaseServer server, string imageName, string dataVolumeClaimName)
        {
            if (server == null)
                throw new ArgumentNullException(nameof(server));

            if (String.IsNullOrWhiteSpace(dataVolumeClaimName))
                throw new ArgumentException("Argument cannot be null, empty, or entirely composed of whitespace: 'dataVolumeClaimName'.", nameof(dataVolumeClaimName));

            string baseName = GetBaseName(server);
            
            return Deployment(
                name: baseName,
                spec: KubeSpecs.Deployment(server, imageName, dataVolumeClaimName)
            );
        }

        /// <summary>
        ///     Create a new <see cref="V1beta1Deployment"/>.
        /// </summary>
        /// <param name="name">
        ///     The deployment name.
        /// </param>
        /// <param name="spec">
        ///     A <see cref="V1beta1DeploymentSpec"/> representing the deployment specification.
        /// </param>
        /// <param name="labels">
        ///     An optional <see cref="Dictionary{TKey, TValue}"/> containing labels to apply to the deployment.
        /// </param>
        /// <param name="annotations">
        ///     An optional <see cref="Dictionary{TKey, TValue}"/> containing annotations to apply to the deployment.
        /// </param>
        /// <returns>
        ///     The configured <see cref="V1beta1Deployment"/>.
        /// </returns>
        public static V1beta1Deployment Deployment(string name, V1beta1DeploymentSpec spec, Dictionary<string, string> labels = null, Dictionary<string, string> annotations = null)
        {
            if (String.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Argument cannot be null, empty, or entirely composed of whitespace: 'name'.", nameof(name));
            
            if (spec == null)
                throw new ArgumentNullException(nameof(spec));

            return new V1beta1Deployment
            {
                ApiVersion = "apps/v1beta1",
                Kind = "Deployment",
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
        ///     Create a new <see cref="V1ReplicationController"/> for the specified database server.
        /// </summary>
        /// <param name="server">
        ///     A <see cref="DatabaseServer"/> representing the target server.
        /// </param>
        /// <param name="imageName">
        ///     The name (and tag) of the SQL Server for Linux image to use.
        /// </param>
        /// <param name="dataVolumeClaimName">
        ///     The name of the Kubernetes VolumeClaim where the data will be stored.
        /// </param>
        /// <returns>
        ///     The configured <see cref="V1ReplicationController"/>.
        /// </returns>
        public static V1ReplicationController ReplicationController(DatabaseServer server, string imageName, string dataVolumeClaimName)
        {
            if (server == null)
                throw new ArgumentNullException(nameof(server));

            if (String.IsNullOrWhiteSpace(imageName))
                throw new ArgumentException("Argument cannot be null, empty, or entirely composed of whitespace: 'imageName'.", nameof(imageName));

            if (String.IsNullOrWhiteSpace(dataVolumeClaimName))
                throw new ArgumentException("Argument cannot be null, empty, or entirely composed of whitespace: 'dataVolumeClaimName'.", nameof(dataVolumeClaimName));

            string baseName = GetBaseName(server);
            
            return ReplicationController(
                name: baseName,
                spec: KubeSpecs.ReplicationController(server, imageName, dataVolumeClaimName)
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
        ///     Create a new internally-facing <see cref="V1Service"/> for the specified database server.
        /// </summary>
        /// <param name="server">
        ///     A <see cref="DatabaseServer"/> representing the target server.
        /// </param>
        /// <returns>
        ///     The configured <see cref="V1Service"/>.
        /// </returns>
        public static V1Service InternalService(DatabaseServer server)
        {
            if (server == null)
                throw new ArgumentNullException(nameof(server));

            string baseName = GetBaseName(server);
            
            return Service(
                name: $"{baseName}-service-internal",
                spec: KubeSpecs.InternalService(server),
                labels: new Dictionary<string, string>
                {
                    ["k8s-app"] = baseName,
                    ["cloud.dimensiondata.daas.server-id"] = server.Id.ToString(),
                    ["cloud.dimensiondata.daas.service-type"] = "internal"
                }
            );
        }

        /// <summary>
        ///     Create a new externally-facing <see cref="V1Service"/> for the specified database server.
        /// </summary>
        /// <param name="server">
        ///     A <see cref="DatabaseServer"/> representing the target server.
        /// </param>
        /// <returns>
        ///     The configured <see cref="V1Service"/>.
        /// </returns>
        public static V1Service ExternalService(DatabaseServer server)
        {
            if (server == null)
                throw new ArgumentNullException(nameof(server));

            string baseName = GetBaseName(server);
            
            return Service(
                name: $"{baseName}-service-external",
                spec: KubeSpecs.ExternalService(server),
                labels: new Dictionary<string, string>
                {
                    ["k8s-app"] = baseName,
                    ["cloud.dimensiondata.daas.server-id"] = server.Id.ToString(),
                    ["cloud.dimensiondata.daas.service-type"] = "external"
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
        ///     Create a new <see cref="V1Job"/>.
        /// </summary>
        /// <param name="name">
        ///     The Job name.
        /// </param>
        /// <param name="spec">
        ///     A <see cref="V1JobSpec"/> representing the Job specification.
        /// </param>
        /// <param name="labels">
        ///     An optional <see cref="Dictionary{TKey, TValue}"/> containing labels to apply to the Job.
        /// </param>
        /// <param name="annotations">
        ///     An optional <see cref="Dictionary{TKey, TValue}"/> containing annotations to apply to the Job.
        /// </param>
        /// <returns>
        ///     The configured <see cref="V1Job"/>.
        /// </returns>
        public static V1Job Job(string name, V1JobSpec spec, Dictionary<string, string> labels = null, Dictionary<string, string> annotations = null)
        {
            if (String.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Argument cannot be null, empty, or entirely composed of whitespace: 'name'.", nameof(name));
            
            if (spec == null)
                throw new ArgumentNullException(nameof(spec));

            return new V1Job
            {
                ApiVersion = "batch/v1",
                Kind = "Job",
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
