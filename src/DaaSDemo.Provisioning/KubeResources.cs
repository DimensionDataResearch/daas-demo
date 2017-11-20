using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;

namespace DaaSDemo.Provisioning
{
    using Common.Options;
    using KubeClient.Models;
    using Messages;
    using Models.Data;

    /// <summary>
    ///     Factory methods for common Kubernetes resources.
    /// </summary>
    public class KubeResources
    {
        /// <summary>
        ///     Create a new <see cref="KubeResources"/>.
        /// </summary>
        /// <param name="names">
        ///     The Kubernetes resource-naming strategy.
        /// </param>
        /// <param name="specs">
        ///     The factory for Kubernetes specifications.
        /// </param>
        /// <param name="kubeOptions">
        ///     Application-level Kubernetes options.
        /// </param>
        public KubeResources(KubeNames names, KubeSpecs specs, IOptions<KubernetesOptions> kubeOptions)
        {
            if (names == null)
                throw new ArgumentNullException(nameof(names));

            if (specs == null)
                throw new ArgumentNullException(nameof(specs));

            if (kubeOptions == null)
                throw new ArgumentNullException(nameof(kubeOptions));
            
            Names = names;
            Specs = specs;
            KubeOptions = kubeOptions.Value;
        }

        /// <summary>
        ///     The factory for Kubernetes specifications.
        /// </summary>
        public KubeNames Names { get; }

        /// <summary>
        ///     The factory for Kubernetes specifications.
        /// </summary>
        public KubeSpecs Specs { get; }

        /// <summary>
        ///     Application-level Kubernetes options.
        /// </summary>
        public KubernetesOptions KubeOptions { get; }

        /// <summary>
        ///     Create a new <see cref="PersistentVolumeClaimV1"/> for the specified database server.
        /// </summary>
        /// <param name="server">
        ///     A <see cref="DatabaseServer"/> representing the target server.
        /// </param>
        /// <param name="requestedSizeMB">
        ///     The requested volume size (in MB).
        /// </param>
        /// <returns>
        ///     The configured <see cref="PersistentVolumeClaimV1"/>.
        /// </returns>
        public PersistentVolumeClaimV1 DataVolumeClaim(DatabaseServer server, int requestedSizeMB)
        {
            if (server == null)
                throw new ArgumentNullException(nameof(server));

            return DataVolumeClaim(
                name: Names.DataVolumeClaim(server),
                spec: Specs.DataVolumeClaim(server, requestedSizeMB),
                labels: new Dictionary<string, string>
                {
                    ["k8s-app"] = Names.BaseName(server),
                    ["cloud.dimensiondata.daas.server-id"] = server.Id.ToString(),
                    ["cloud.dimensiondata.daas.volume-type"] = "data"
                }
            );
        }

        /// <summary>
        ///     Create a new <see cref="PersistentVolumeClaimV1"/>.
        /// </summary>
        /// <param name="name">
        ///     The deployment name.
        /// </param>
        /// <param name="spec">
        ///     A <see cref="PersistentVolumeClaimSpecV1"/> representing the persistent volume claim specification.
        /// </param>
        /// <param name="labels">
        ///     An optional <see cref="Dictionary{TKey, TValue}"/> containing labels to apply to the persistent volume claim.
        /// </param>
        /// <param name="annotations">
        ///     An optional <see cref="Dictionary{TKey, TValue}"/> containing annotations to apply to the persistent volume claim.
        /// </param>
        /// <returns>
        ///     The configured <see cref="PersistentVolumeClaimV1"/>.
        /// </returns>
        public PersistentVolumeClaimV1 DataVolumeClaim(string name, PersistentVolumeClaimSpecV1 spec, Dictionary<string, string> labels = null, Dictionary<string, string> annotations = null)
        {
            if (String.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Argument cannot be null, empty, or entirely composed of whitespace: 'name'.", nameof(name));
            
            if (spec == null)
                throw new ArgumentNullException(nameof(spec));

            return new PersistentVolumeClaimV1
            {
                ApiVersion = "v1",
                Kind = "PersistentVolumeClaim",
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
        ///     Create a new <see cref="DeploymentV1Beta1"/> for the specified database server.
        /// </summary>
        /// <param name="server">
        ///     A <see cref="DatabaseServer"/> representing the target server.
        /// </param>
        /// <returns>
        ///     The configured <see cref="DeploymentV1Beta1"/>.
        /// </returns>
        public DeploymentV1Beta1 Deployment(DatabaseServer server)
        {
            if (server == null)
                throw new ArgumentNullException(nameof(server));

            string baseName = Names.BaseName(server);
            
            return Deployment(
                name: baseName,
                spec: Specs.Deployment(server),
                labels: new Dictionary<string, string>
                {
                    ["k8s-app"] = baseName,
                    ["cloud.dimensiondata.daas.server-id"] = server.Id.ToString()
                }
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
        public DeploymentV1Beta1 Deployment(string name, DeploymentSpecV1Beta1 spec, Dictionary<string, string> labels = null, Dictionary<string, string> annotations = null)
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
        public ReplicationControllerV1 ReplicationController(DatabaseServer server)
        {
            if (server == null)
                throw new ArgumentNullException(nameof(server));

            string baseName = Names.BaseName(server);
            
            return ReplicationController(
                name: baseName,
                spec: Specs.ReplicationController(server),
                labels: new Dictionary<string, string>
                {
                    ["k8s-app"] = baseName,
                    ["cloud.dimensiondata.daas.server-id"] = server.Id.ToString()
                }
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
        public ReplicationControllerV1 ReplicationController(string name, ReplicationControllerSpecV1 spec, Dictionary<string, string> labels = null, Dictionary<string, string> annotations = null)
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
        public ServiceV1 InternalService(DatabaseServer server)
        {
            if (server == null)
                throw new ArgumentNullException(nameof(server));

            string baseName = Names.BaseName(server);
            
            return Service(
                name: $"{baseName}",
                spec: Specs.InternalService(server),
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
        public ServiceV1 ExternalService(DatabaseServer server)
        {
            if (server == null)
                throw new ArgumentNullException(nameof(server));

            string baseName = Names.BaseName(server);
            
            return Service(
                name: $"{baseName}-public",
                spec: Specs.ExternalService(server),
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
        public ServiceV1 Service(string name, ServiceSpecV1 spec, Dictionary<string, string> labels = null, Dictionary<string, string> annotations = null)
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
        ///     Create a new <see cref="PrometheusServiceMonitorV1"/> for the specified database server.
        /// </summary>
        /// <param name="server">
        ///     A <see cref="DatabaseServer"/> representing the target server.
        /// </param>
        /// <returns>
        ///     The configured <see cref="PrometheusServiceMonitorV1"/>.
        /// </returns>
        public PrometheusServiceMonitorV1 ServiceMonitor(DatabaseServer server)
        {
            if (server == null)
                throw new ArgumentNullException(nameof(server));

            string baseName = Names.BaseName(server);
            
            return ServiceMonitor(
                name: $"{baseName}-monitor",
                spec: Specs.ServiceMonitor(server),
                labels: new Dictionary<string, string>
                {
                    ["k8s-app"] = baseName,
                    ["cloud.dimensiondata.daas.server-id"] = server.Id.ToString(),
                    ["cloud.dimensiondata.daas.monitor-type"] = "sql-server"
                }
            );
        }

        /// <summary>
        ///     Create a new <see cref="PrometheusServiceMonitorV1"/>.
        /// </summary>
        /// <param name="name">
        ///     The service monitor name.
        /// </param>
        /// <param name="spec">
        ///     A <see cref="PrometheusServiceMonitorSpecV1"/> representing the service monitor specification.
        /// </param>
        /// <param name="labels">
        ///     An optional <see cref="Dictionary{TKey, TValue}"/> containing labels to apply to the service.
        /// </param>
        /// <param name="annotations">
        ///     An optional <see cref="Dictionary{TKey, TValue}"/> containing annotations to apply to the service.
        /// </param>
        /// <returns>
        ///     The configured <see cref="PrometheusServiceMonitorV1"/>.
        /// </returns>
        public PrometheusServiceMonitorV1 ServiceMonitor(string name, PrometheusServiceMonitorSpecV1 spec, Dictionary<string, string> labels = null, Dictionary<string, string> annotations = null)
        {
            if (String.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Argument cannot be null, empty, or entirely composed of whitespace: 'name'.", nameof(name));
            
            if (spec == null)
                throw new ArgumentNullException(nameof(spec));

            return new PrometheusServiceMonitorV1
            {
                ApiVersion = "monitoring.coreos.com/v1",
                Kind = "ServiceMonitor",
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
        public JobV1 Job(string name, JobSpecV1 spec, Dictionary<string, string> labels = null, Dictionary<string, string> annotations = null)
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
