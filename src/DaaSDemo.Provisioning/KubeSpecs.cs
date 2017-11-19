using System;
using System.Collections.Generic;

namespace DaaSDemo.Provisioning
{
    using KubeClient.Models;
    using Models.Data;

    /// <summary>
    ///     Factory methods for common Kubernetes resource specifications.
    /// </summary>
    public static class KubeSpecs
    {
        /// <summary>
        ///     Build a <see cref="DeploymentSpecV1Beta1"/> for the specified server.
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
        ///     The configured <see cref="DeploymentSpecV1Beta1"/>.
        /// </returns>
        public static DeploymentSpecV1Beta1 Deployment(DatabaseServer server, string imageName, string dataVolumeClaimName)
        {
            if (server == null)
                throw new ArgumentNullException(nameof(server));

            if (String.IsNullOrWhiteSpace(imageName))
                throw new ArgumentException("Argument cannot be null, empty, or entirely composed of whitespace: 'imageName'.", nameof(imageName));
            
            if (String.IsNullOrWhiteSpace(dataVolumeClaimName))
                throw new ArgumentException("Argument cannot be null, empty, or entirely composed of whitespace: 'dataVolumeClaimName'.", nameof(dataVolumeClaimName));

            string baseName = KubeResources.GetBaseName(server);

            return new DeploymentSpecV1Beta1
            {
                Replicas = 1,
                MinReadySeconds = 30,
                Strategy = new DeploymentStrategyV1Beta1
                {
                    Type = "Recreate" // Shut down the old instance before starting the new one
                },
                Selector = new LabelSelectorV1
                {
                    MatchLabels = new Dictionary<string, string>
                    {
                        ["k8s-app"] = baseName
                    }
                },
                Template = new PodTemplateSpecV1
                {
                    Metadata = new ObjectMetaV1
                    {
                        Labels = new Dictionary<string, string>
                        {
                            ["k8s-app"] = baseName,
                            ["cloud.dimensiondata.daas.server-id"] = server.Id.ToString() // TODO: Use tenant Id instead
                        }
                    },
                    Spec = new PodSpecV1
                    {
                        TerminationGracePeriodSeconds = 60,
                        ImagePullSecrets = new List<LocalObjectReferenceV1>
                        {
                            new LocalObjectReferenceV1
                            {
                                Name = "daas-registry"
                            }
                        },
                        Containers = new List<ContainerV1>
                        {
                            new ContainerV1
                            {
                                Name = baseName,
                                Image = imageName,
                                Resources = new ResourceRequirementsV1
                                {
                                    Requests = new Dictionary<string, string>
                                    {
                                        ["memory"] = "4Gi" // SQL Server for Linux requires at least 4 GB of RAM
                                    },
                                    Limits = new Dictionary<string, string>
                                    {
                                        ["memory"] = "6Gi" // If you're using more than 6 GB of RAM, then you should probably host stand-alone
                                    }
                                },
                                Env = new List<EnvVarV1>
                                {
                                    new EnvVarV1
                                    {
                                        Name = "ACCEPT_EULA",
                                        Value = "Y"
                                    },
                                    new EnvVarV1
                                    {
                                        Name = "SA_PASSWORD",
                                        Value = server.AdminPassword // TODO: Use Secret resource instead.
                                    }
                                },
                                Ports = new List<ContainerPortV1>
                                {
                                    new ContainerPortV1
                                    {
                                        ContainerPort = 1433
                                    }
                                },
                                VolumeMounts = new List<VolumeMountV1>
                                {
                                    new VolumeMountV1
                                    {
                                        Name = "sql-data",
                                        SubPath = baseName,
                                        MountPath = "/var/opt/mssql"
                                    }
                                }
                            }
                        },
                        Volumes = new List<VolumeV1>
                        {
                            new VolumeV1
                            {
                                Name = "sql-data",
                                PersistentVolumeClaim = new PersistentVolumeClaimVolumeSourceV1
                                {
                                    ClaimName = dataVolumeClaimName
                                }
                            }
                        }
                    }
                }
            };
        }

        /// <summary>
        ///     Build a <see cref="ReplicationControllerSpecV1"/> for the specified server.
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
        ///     The configured <see cref="ReplicationControllerSpecV1"/>.
        /// </returns>
        public static ReplicationControllerSpecV1 ReplicationController(DatabaseServer server, string imageName, string dataVolumeClaimName)
        {
            if (server == null)
                throw new ArgumentNullException(nameof(server));

            if (String.IsNullOrWhiteSpace(imageName))
                throw new ArgumentException("Argument cannot be null, empty, or entirely composed of whitespace: 'imageName'.", nameof(imageName));
            
            if (String.IsNullOrWhiteSpace(dataVolumeClaimName))
                throw new ArgumentException("Argument cannot be null, empty, or entirely composed of whitespace: 'dataVolumeClaimName'.", nameof(dataVolumeClaimName));

            string baseName = KubeResources.GetBaseName(server);

            return new ReplicationControllerSpecV1
            {
                Replicas = 1,
                MinReadySeconds = 30,
                Selector = new Dictionary<string, string>
                {
                    ["k8s-app"] = baseName
                },
                Template = new PodTemplateSpecV1
                {
                    Metadata = new ObjectMetaV1
                    {
                        Labels = new Dictionary<string, string>
                        {
                            ["k8s-app"] = baseName,
                            ["cloud.dimensiondata.daas.server-id"] = server.Id.ToString() // TODO: Use tenant Id instead
                        }
                    },
                    Spec = new PodSpecV1
                    {
                        TerminationGracePeriodSeconds = 60,
                        ImagePullSecrets = new List<LocalObjectReferenceV1>
                        {
                            new LocalObjectReferenceV1
                            {
                                Name = "daas-registry"
                            }
                        },
                        Containers = new List<ContainerV1>
                        {
                            // SQL Server
                            new ContainerV1
                            {
                                Name = "sql-server",
                                Image = imageName,
                                Resources = new ResourceRequirementsV1
                                {
                                    Requests = new Dictionary<string, string>
                                    {
                                        ["memory"] = "4Gi" // SQL Server for Linux requires at least 4 GB of RAM
                                    },
                                    Limits = new Dictionary<string, string>
                                    {
                                        ["memory"] = "6Gi" // If you're using more than 6 GB of RAM, then you should probably host stand-alone
                                    }
                                },
                                Env = new List<EnvVarV1>
                                {
                                    new EnvVarV1
                                    {
                                        Name = "ACCEPT_EULA",
                                        Value = "Y"
                                    },
                                    new EnvVarV1
                                    {
                                        Name = "SA_PASSWORD",
                                        Value = server.AdminPassword // TODO: Use Secret resource instead.
                                    }
                                },
                                Ports = new List<ContainerPortV1>
                                {
                                    new ContainerPortV1
                                    {
                                        ContainerPort = 1433
                                    }
                                },
                                VolumeMounts = new List<VolumeMountV1>
                                {
                                    new VolumeMountV1
                                    {
                                        Name = "sql-data",
                                        SubPath = baseName,
                                        MountPath = "/var/opt/mssql"
                                    }
                                }
                            },
                            // Prometheus exporter
                            new ContainerV1
                            {
                                Name = "prometheus-exporter",
                                Image = "tintoy.azurecr.io/daas/prometheus-mssql-exporter:1.0.0-dev", // TODO: Make this configurable.
                                Env = new List<EnvVarV1>
                                {
                                    new EnvVarV1
                                    {
                                        Name = "SERVER",
                                        Value = "127.0.0.1",
                                    },
                                    new EnvVarV1
                                    {
                                        Name = "USERNAME",
                                        Value = "sa" // TODO: Use Secret resource instead.
                                    },
                                    new EnvVarV1
                                    {
                                        Name = "PASSWORD",
                                        Value = server.AdminPassword // TODO: Use Secret resource instead.
                                    },
                                    new EnvVarV1
                                    {
                                        Name = "DEBUG",
                                        Value = "app"
                                    }
                                },
                                Ports = new List<ContainerPortV1>
                                {
                                    new ContainerPortV1
                                    {
                                        ContainerPort = 4000
                                    }
                                }
                            }
                        },
                        Volumes = new List<VolumeV1>
                        {
                            new VolumeV1
                            {
                                Name = "sql-data",
                                PersistentVolumeClaim = new PersistentVolumeClaimVolumeSourceV1
                                {
                                    ClaimName = dataVolumeClaimName
                                }
                            }
                        }
                    }
                }
            };
        }

        /// <summary>
        ///     Build an internally-facing <see cref="ServiceSpecV1"/> for the specified server.
        /// </summary>
        /// <param name="server">
        ///     A <see cref="DatabaseServer"/> representing the target server.
        /// </param>
        /// <returns>
        ///     The configured <see cref="ServiceSpecV1"/>.
        /// </returns>
        public static ServiceSpecV1 InternalService(DatabaseServer server)
        {
            if (server == null)
                throw new ArgumentNullException(nameof(server));

            string baseName = KubeResources.GetBaseName(server);

            return new ServiceSpecV1
            {
                Ports = new List<ServicePortV1>
                {
                    new ServicePortV1
                    {
                        Name = "sql-server",
                        Port = 1433,
                        Protocol = "TCP"
                    },
                    new ServicePortV1
                    {
                        Name = "prometheus-exporter",
                        Port = 4000,
                        Protocol = "TCP"
                    }
                },
                Selector = new Dictionary<string, string>
                {
                    ["k8s-app"] = baseName
                }
            };
        }

        /// <summary>
        ///     Build an externally-facing <see cref="ServiceSpecV1"/> for the specified server.
        /// </summary>
        /// <param name="server">
        ///     A <see cref="DatabaseServer"/> representing the target server.
        /// </param>
        /// <returns>
        ///     The configured <see cref="ServiceSpecV1"/>.
        /// </returns>
        public static ServiceSpecV1 ExternalService(DatabaseServer server)
        {
            if (server == null)
                throw new ArgumentNullException(nameof(server));

            string baseName = KubeResources.GetBaseName(server);

            return new ServiceSpecV1
            {
                Type = "NodePort",
                Ports = new List<ServicePortV1>
                {
                    new ServicePortV1
                    {
                        Name = "sql-server",
                        Port = 1433,
                        Protocol = "TCP"
                    }
                },
                Selector = new Dictionary<string, string>
                {
                    ["k8s-app"] = baseName
                }
            };
        }

        /// <summary>
        ///     Build a <see cref="PrometheusServiceMonitorSpecV1"/> for the specified server.
        /// </summary>
        /// <param name="server">
        ///     A <see cref="DatabaseServer"/> representing the target server.
        /// </param>
        /// <returns>
        ///     The configured <see cref="PrometheusServiceMonitorSpecV1"/>.
        /// </returns>
        public static PrometheusServiceMonitorSpecV1 ServiceMonitor(DatabaseServer server)
        {
            if (server == null)
                throw new ArgumentNullException(nameof(server));

            string baseName = KubeResources.GetBaseName(server);

            return new PrometheusServiceMonitorSpecV1
            {
                JobLabel = baseName,
                Selector = new LabelSelectorV1
                {
                    MatchLabels = new Dictionary<string, string>
                    {
                        ["cloud.dimensiondata.daas.server-id"] = server.Id.ToString(),
                        ["cloud.dimensiondata.daas.service-type"] = "internal"
                    }
                },
                EndPoints = new List<PrometheusServiceMonitorEndPointV1>
                {
                    new PrometheusServiceMonitorEndPointV1
                    {
                        Port = "prometheus-exporter"
                    }
                }
            };
        }
    }
}
