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
        ///     Create a new <see cref="DeploymentV1Beta1"/> for the specified database server.
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
        ///     The configured <see cref="DeploymentV1Beta1"/>.
        /// </returns>
        public static DeploymentV1Beta1 Deployment(DatabaseServer server, string imageName, string dataVolumeClaimName)
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
        ///     Create a new <see cref="DeploymentV1Beta1"/>.
        /// </summary>
        /// <param name="name">
        ///     The deployment name.
        /// </param>
        /// <param name="spec">
        ///     A <see cref="DeploymentSpecV1Beta1"/> representing the deployment specification.
        /// </param>
        /// <param name="labels">
        ///     An optional <see cref="Dictionary{TKey, TValue}"/> containing labels to apply to the deployment.
        /// </param>
        /// <param name="annotations">
        ///     An optional <see cref="Dictionary{TKey, TValue}"/> containing annotations to apply to the deployment.
        /// </param>
        /// <returns>
        ///     The configured <see cref="DeploymentV1Beta1"/>.
        /// </returns>
        public static DeploymentV1Beta1 Deployment(string name, DeploymentSpecV1Beta1 spec, Dictionary<string, string> labels = null, Dictionary<string, string> annotations = null)
        {
            if (String.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Argument cannot be null, empty, or entirely composed of whitespace: 'name'.", nameof(name));
            
            if (spec == null)
                throw new ArgumentNullException(nameof(spec));

            return new DeploymentV1Beta1
            {
                ApiVersion = "apps/v1beta1",
                Kind = "Deployment",
                Metadata = new ObjectMetaV1
                {
                    Name = name,
                    Labels = labels,
                    Annotations = annotations
                },
                Spec = spec
            };
        }

        /// <summary>
        ///     Create a new <see cref="ReplicationControllerV1"/> for the specified database server.
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
        ///     The configured <see cref="ReplicationControllerV1"/>.
        /// </returns>
        public static ReplicationControllerV1 ReplicationController(DatabaseServer server, string imageName, string dataVolumeClaimName)
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
        ///     Create a new <see cref="ReplicationControllerV1"/>.
        /// </summary>
        /// <param name="name">
        ///     The replication controller name.
        /// </param>
        /// <param name="spec">
        ///     A <see cref="ReplicationControllerSpecV1"/> representing the controller specification.
        /// </param>
        /// <param name="labels">
        ///     An optional <see cref="Dictionary{TKey, TValue}"/> containing labels to apply to the replication controller.
        /// </param>
        /// <param name="annotations">
        ///     An optional <see cref="Dictionary{TKey, TValue}"/> containing annotations to apply to the replication controller.
        /// </param>
        /// <returns>
        ///     The configured <see cref="ReplicationControllerV1"/>.
        /// </returns>
        public static ReplicationControllerV1 ReplicationController(string name, ReplicationControllerSpecV1 spec, Dictionary<string, string> labels = null, Dictionary<string, string> annotations = null)
        {
            if (String.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Argument cannot be null, empty, or entirely composed of whitespace: 'name'.", nameof(name));
            
            if (spec == null)
                throw new ArgumentNullException(nameof(spec));

            return new ReplicationControllerV1
            {
                ApiVersion = "v1",
                Kind = "ReplicationController",
                Metadata = new ObjectMetaV1
                {
                    Name = name,
                    Labels = labels,
                    Annotations = annotations
                },
                Spec = spec
            };
        }

        /// <summary>
        ///     Create a new internally-facing <see cref="ServiceV1"/> for the specified database server.
        /// </summary>
        /// <param name="server">
        ///     A <see cref="DatabaseServer"/> representing the target server.
        /// </param>
        /// <returns>
        ///     The configured <see cref="ServiceV1"/>.
        /// </returns>
        public static ServiceV1 InternalService(DatabaseServer server)
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
        ///     Create a new externally-facing <see cref="ServiceV1"/> for the specified database server.
        /// </summary>
        /// <param name="server">
        ///     A <see cref="DatabaseServer"/> representing the target server.
        /// </param>
        /// <returns>
        ///     The configured <see cref="ServiceV1"/>.
        /// </returns>
        public static ServiceV1 ExternalService(DatabaseServer server)
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
        ///     Create a new <see cref="ServiceV1"/>.
        /// </summary>
        /// <param name="name">
        ///     The service name.
        /// </param>
        /// <param name="spec">
        ///     A <see cref="ServiceSpecV1"/> representing the service specification.
        /// </param>
        /// <param name="labels">
        ///     An optional <see cref="Dictionary{TKey, TValue}"/> containing labels to apply to the service.
        /// </param>
        /// <param name="annotations">
        ///     An optional <see cref="Dictionary{TKey, TValue}"/> containing annotations to apply to the service.
        /// </param>
        /// <returns>
        ///     The configured <see cref="ServiceV1"/>.
        /// </returns>
        public static ServiceV1 Service(string name, ServiceSpecV1 spec, Dictionary<string, string> labels = null, Dictionary<string, string> annotations = null)
        {
            if (String.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Argument cannot be null, empty, or entirely composed of whitespace: 'name'.", nameof(name));
            
            if (spec == null)
                throw new ArgumentNullException(nameof(spec));

            return new ServiceV1
            {
                ApiVersion = "v1",
                Kind = "Service",
                Metadata = new ObjectMetaV1
                {
                    Name = name,
                    Labels = labels,
                    Annotations = annotations
                },
                Spec = spec
            };
        }

        /// <summary>
        ///     Create a new <see cref="JobV1"/>.
        /// </summary>
        /// <param name="name">
        ///     The Job name.
        /// </param>
        /// <param name="spec">
        ///     A <see cref="JobSpecV1"/> representing the Job specification.
        /// </param>
        /// <param name="labels">
        ///     An optional <see cref="Dictionary{TKey, TValue}"/> containing labels to apply to the Job.
        /// </param>
        /// <param name="annotations">
        ///     An optional <see cref="Dictionary{TKey, TValue}"/> containing annotations to apply to the Job.
        /// </param>
        /// <returns>
        ///     The configured <see cref="JobV1"/>.
        /// </returns>
        public static JobV1 Job(string name, JobSpecV1 spec, Dictionary<string, string> labels = null, Dictionary<string, string> annotations = null)
        {
            if (String.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Argument cannot be null, empty, or entirely composed of whitespace: 'name'.", nameof(name));
            
            if (spec == null)
                throw new ArgumentNullException(nameof(spec));

            return new JobV1
            {
                ApiVersion = "batch/v1",
                Kind = "Job",
                Metadata = new ObjectMetaV1
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
