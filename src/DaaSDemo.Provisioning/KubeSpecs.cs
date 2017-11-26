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
        ///     Application-level Kubernetes settings.
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
        ///     Application-level Kubernetes settings.
        /// </summary>
        public KubeNames Names { get; }

        /// <summary>
        ///     Application-level Kubernetes settings.
        /// </summary>
        public KubernetesOptions KubeOptions { get; }

        /// <summary>
        ///     Application-level provisioning options.
        /// </summary>
        public ProvisioningOptions ProvisioningOptions { get; }

        /// <summary>
        ///     Build a <see cref="PersistentVolumeClaimSpecV1"/> for the specified server.
        /// </summary>
        /// <param name="server">
        ///     A <see cref="DatabaseServer"/> representing the target server.
        /// </param>
        /// <returns>
        ///     The configured <see cref="PersistentVolumeClaimSpecV1"/>.
        /// </returns>
        public PersistentVolumeClaimSpecV1 DataVolumeClaim(DatabaseServer server)
        {
            if (server == null)
                throw new ArgumentNullException(nameof(server));
            
            return new PersistentVolumeClaimSpecV1
            {
                AccessModes = new List<string>
                {
                    "ReadWriteOnce"
                },
                StorageClassName = KubeOptions.DatabaseStorageClass,
                Resources = new ResourceRequirementsV1
                {
                    Requests = new Dictionary<string, string>
                    {
                        ["storage"] = $"{server.Storage.SizeMB}Mi"
                    }
                }
            };
        }

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

            var deploymentSpec = new DeploymentSpecV1Beta1
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
                            ["cloud.dimensiondata.daas.server-id"] = server.Id, // TODO: Use tenant Id instead
                            ["cloud.dimensiondata.daas.server-kind"] = server.Kind.ToString()
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
                        Containers = new List<ContainerV1>(),
                        Volumes = new List<VolumeV1>
                        {
                            new VolumeV1
                            {
                                Name = "data",
                                PersistentVolumeClaim = new PersistentVolumeClaimVolumeSourceV1
                                {
                                    ClaimName = Names.DataVolumeClaim(server)
                                }
                            }
                        }
                    }
                }
            };

            PodSpecV1 podSpec = deploymentSpec.Template.Spec;
            switch (server.Kind)
            {
                case DatabaseServerKind.SqlServer:
                {
                    // SQL Server
                    podSpec.Containers.Add(new ContainerV1
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
                                Name = "data",
                                SubPath = baseName,
                                MountPath = "/var/opt/mssql"
                            }
                        }
                    });
                        
                    // Prometheus exporter
                    podSpec.Containers.Add(new ContainerV1
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
                    });

                    break;
                }
                case DatabaseServerKind.RavenDB:
                {
                    podSpec.Containers.Add(new ContainerV1
                    {
                        Name = "ravendb",
                        Image = ProvisioningOptions.Images.RavenDB,
                        Resources = new ResourceRequirementsV1
                        {
                            Requests = new Dictionary<string, string>
                            {
                                ["memory"] = "1Gi"
                            },
                            Limits = new Dictionary<string, string>
                            {
                                ["memory"] = "3Gi"
                            }
                        },
                        Env = new List<EnvVarV1>
                        {
                            new EnvVarV1
                            {
                                Name = "UNSECURED_ACCESS_ALLOWED",
                                Value = "PublicNetwork"
                            }
                        },
                        Ports = new List<ContainerPortV1>
                        {
                            new ContainerPortV1
                            {
                                Name = "http",
                                ContainerPort = 8080
                            },
                            new ContainerPortV1
                            {
                                Name = "tcp",
                                ContainerPort = 38888
                            }
                        },
                        VolumeMounts = new List<VolumeMountV1>
                        {
                            new VolumeMountV1
                            {
                                Name = "data",
                                SubPath = baseName,
                                MountPath = "/databases"
                            }
                        }
                    });

                    break;
                }
                default:
                {
                    throw new NotSupportedException($"Unsupported server kind ({server.Kind}).");
                }
            }

            return deploymentSpec;
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
                            ["cloud.dimensiondata.daas.server-id"] = server.Id
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
                                        Name = "data",
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
                                Name = "data",
                                PersistentVolumeClaim = new PersistentVolumeClaimVolumeSourceV1
                                {
                                    ClaimName = Names.DataVolumeClaim(server)
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

            var spec = new ServiceSpecV1
            {
                Ports = new List<ServicePortV1>(),
                Selector = new Dictionary<string, string>
                {
                    ["k8s-app"] = baseName
                }
            };

            switch (server.Kind)
            {
                case DatabaseServerKind.SqlServer:
                {
                    spec.Ports.Add(new ServicePortV1
                    {
                        Name = "sql-server",
                        Port = 1433,
                        Protocol = "TCP"
                    });
                    spec.Ports.Add(new ServicePortV1
                    {
                        Name = "prometheus-exporter",
                        Port = 4000,
                        Protocol = "TCP"
                    });

                    break;
                }
                case DatabaseServerKind.RavenDB:
                {
                    spec.Ports.Add(new ServicePortV1
                    {
                        Name = "http",
                        Port = 8080,
                        Protocol = "TCP"
                    });
                    spec.Ports.Add(new ServicePortV1
                    {
                        Name = "tcp",
                        Port = 38888,
                        Protocol = "TCP"
                    });

                    break;
                }
                default:
                {
                    throw new NotSupportedException($"Unsupported server type ({server.Kind}).");
                }
            }

            return spec;
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

            var spec = new ServiceSpecV1
            {
                Type = "NodePort",
                Ports = new List<ServicePortV1>(),
                Selector = new Dictionary<string, string>
                {
                    ["k8s-app"] = baseName
                }
            };

            switch (server.Kind)
            {
                case DatabaseServerKind.SqlServer:
                {
                    spec.Ports.Add(new ServicePortV1
                    {
                        Name = "sql-server",
                        Port = 1433,
                        Protocol = "TCP"
                    });

                    break;
                }
                case DatabaseServerKind.RavenDB:
                {
                    spec.Ports.Add(new ServicePortV1
                    {
                        Name = "http",
                        Port = 8080,
                        Protocol = "TCP"
                    });
                    spec.Ports.Add(new ServicePortV1
                    {
                        Name = "tcp",
                        Port = 38888,
                        Protocol = "TCP"
                    });

                    break;
                }
                default:
                {
                    throw new NotSupportedException($"Unsupported server type ({server.Kind}).");
                }
            }

            return spec;
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
                        ["cloud.dimensiondata.daas.server-id"] = server.Id,
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
