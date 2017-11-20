using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;

namespace DaaSDemo.Provisioning
{
    using Common.Options;
    using KubeClient.Models;
    using Models.Data;

    /// <summary>
    ///     Factory methods for common Kubernetes resource specifications.
    /// </summary>
    public class KubeSpecs
    {
        /// <summary>
        ///     Create a new <see cref="KubeResources"/>.
        /// </summary>
        /// <param name="names">
        ///     The Kubernetes resource-naming strategy.
        /// </param>
        /// <param name="kubeOptions">
        ///     Application-level Kubernetes options.
        /// </param>
        /// <param name="provisioningOptions">
        ///     Application-level provisioning options.
        /// </param>
        public KubeSpecs(KubeNames names, IOptions<KubernetesOptions> kubeOptions, IOptions<ProvisioningOptions> provisioningOptions)
        {
            if (names == null)
                throw new ArgumentNullException(nameof(names));

            if (kubeOptions == null)
                throw new ArgumentNullException(nameof(kubeOptions));

            if (provisioningOptions == null)
                throw new ArgumentNullException(nameof(provisioningOptions));

            Names = names;
            KubeOptions = kubeOptions.Value;
            ProvisioningOptions = provisioningOptions.Value;
        }

        /// <summary>
        ///     Application-level Kubernetes options.
        /// </summary>
        public KubeNames Names { get; }

        /// <summary>
        ///     Application-level Kubernetes options.
        /// </summary>
        public KubernetesOptions KubeOptions { get; }

        /// <summary>
        ///     Application-level provisioning options.
        /// </summary>
        public ProvisioningOptions ProvisioningOptions { get; }

        /// <summary>
        ///     Build a <see cref="DeploymentSpecV1Beta1"/> for the specified server.
        /// </summary>
        /// <param name="server">
        ///     A <see cref="DatabaseServer"/> representing the target server.
        /// </param>
        /// <returns>
        ///     The configured <see cref="DeploymentSpecV1Beta1"/>.
        /// </returns>
        public DeploymentSpecV1Beta1 Deployment(DatabaseServer server)
        {
            if (server == null)
                throw new ArgumentNullException(nameof(server));

            string baseName = Names.BaseName(server);

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
                            // SQL Server
                            new ContainerV1
                            {
                                Name = "sql-server",
                                Image = ProvisioningOptions.Images.SQL,
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
                                Image = ProvisioningOptions.Images.SQLExporter,
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
                                    ClaimName = KubeOptions.VolumeClaimName
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
        /// <returns>
        ///     The configured <see cref="ReplicationControllerSpecV1"/>.
        /// </returns>
        public ReplicationControllerSpecV1 ReplicationController(DatabaseServer server)
        {
            if (server == null)
                throw new ArgumentNullException(nameof(server));

            string baseName = Names.BaseName(server);

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
                            ["cloud.dimensiondata.daas.server-id"] = server.Id.ToString()
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
                                Image = ProvisioningOptions.Images.SQL,
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
                                Image = ProvisioningOptions.Images.SQLExporter,
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
                                    ClaimName = KubeOptions.VolumeClaimName
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
        public ServiceSpecV1 InternalService(DatabaseServer server)
        {
            if (server == null)
                throw new ArgumentNullException(nameof(server));

            string baseName = Names.BaseName(server);

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
        public ServiceSpecV1 ExternalService(DatabaseServer server)
        {
            if (server == null)
                throw new ArgumentNullException(nameof(server));

            string baseName = Names.BaseName(server);

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
        public PrometheusServiceMonitorSpecV1 ServiceMonitor(DatabaseServer server)
        {
            if (server == null)
                throw new ArgumentNullException(nameof(server));

            string baseName = Names.BaseName(server);

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
