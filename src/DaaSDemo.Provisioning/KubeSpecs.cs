using KubeNET.Swagger.Model;
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
        ///     Build a <see cref="V1beta1DeploymentSpec"/> for the specified server.
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
        ///     The configured <see cref="V1beta1DeploymentSpec"/>.
        /// </returns>
        public static V1beta1DeploymentSpec Deployment(DatabaseServer server, string imageName, string dataVolumeClaimName)
        {
            if (server == null)
                throw new ArgumentNullException(nameof(server));

            if (String.IsNullOrWhiteSpace(imageName))
                throw new ArgumentException("Argument cannot be null, empty, or entirely composed of whitespace: 'imageName'.", nameof(imageName));
            
            if (String.IsNullOrWhiteSpace(dataVolumeClaimName))
                throw new ArgumentException("Argument cannot be null, empty, or entirely composed of whitespace: 'dataVolumeClaimName'.", nameof(dataVolumeClaimName));

            string baseName = KubeResources.GetBaseName(server);

            return new V1beta1DeploymentSpec
            {
                Replicas = 1,
                MinReadySeconds = 30,
                Strategy = new V1beta1DeploymentStrategy
                {
                    Type = "Recreate" // Shut down the old instance before starting the new one
                },
                Selector = new V1beta1LabelSelector
                {
                    MatchLabels = new Dictionary<string, string>
                    {
                        ["k8s-app"] = baseName
                    }
                },
                Template = new V1PodTemplateSpec
                {
                    Metadata = new V1ObjectMeta
                    {
                        Labels = new Dictionary<string, string>
                        {
                            ["k8s-app"] = baseName,
                            ["cloud.dimensiondata.daas.server-id"] = server.Id.ToString() // TODO: Use tenant Id instead
                        }
                    },
                    Spec = new V1PodSpec
                    {
                        TerminationGracePeriodSeconds = 60,
                        Containers = new List<V1Container>
                        {
                            new V1Container
                            {
                                Name = baseName,
                                Image = imageName,
                                Resources = new V1ResourceRequirements
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
                                Env = new List<V1EnvVar>
                                {
                                    new V1EnvVar
                                    {
                                        Name = "ACCEPT_EULA",
                                        Value = "Y"
                                    },
                                    new V1EnvVar
                                    {
                                        Name = "SA_PASSWORD",
                                        Value = server.AdminPassword // TODO: Use Secret resource instead.
                                    }
                                },
                                Ports = new List<V1ContainerPort>
                                {
                                    new V1ContainerPort
                                    {
                                        ContainerPort = 1433
                                    }
                                },
                                VolumeMounts = new List<V1VolumeMount>
                                {
                                    new V1VolumeMount
                                    {
                                        Name = "sql-data",
                                        SubPath = baseName,
                                        MountPath = "/var/opt/mssql"
                                    }
                                }
                            }
                        },
                        Volumes = new List<V1Volume>
                        {
                            new V1Volume
                            {
                                Name = "sql-data",
                                PersistentVolumeClaim = new V1PersistentVolumeClaimVolumeSource
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
        ///     Build a <see cref="V1ReplicationControllerSpec"/> for the specified server.
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
        ///     The configured <see cref="V1ReplicationControllerSpec"/>.
        /// </returns>
        public static V1ReplicationControllerSpec ReplicationController(DatabaseServer server, string imageName, string dataVolumeClaimName)
        {
            if (server == null)
                throw new ArgumentNullException(nameof(server));

            if (String.IsNullOrWhiteSpace(imageName))
                throw new ArgumentException("Argument cannot be null, empty, or entirely composed of whitespace: 'imageName'.", nameof(imageName));
            
            if (String.IsNullOrWhiteSpace(dataVolumeClaimName))
                throw new ArgumentException("Argument cannot be null, empty, or entirely composed of whitespace: 'dataVolumeClaimName'.", nameof(dataVolumeClaimName));

            string baseName = KubeResources.GetBaseName(server);

            return new V1ReplicationControllerSpec
            {
                Replicas = 1,
                MinReadySeconds = 30,
                Selector = new Dictionary<string, string>
                {
                    ["k8s-app"] = baseName
                },
                Template = new V1PodTemplateSpec
                {
                    Metadata = new V1ObjectMeta
                    {
                        Labels = new Dictionary<string, string>
                        {
                            ["k8s-app"] = baseName,
                            ["cloud.dimensiondata.daas.server-id"] = server.Id.ToString() // TODO: Use tenant Id instead
                        }
                    },
                    Spec = new V1PodSpec
                    {
                        TerminationGracePeriodSeconds = 60,
                        Containers = new List<V1Container>
                        {
                            // SQL Server
                            new V1Container
                            {
                                Name = "sql-server",
                                Image = imageName,
                                Resources = new V1ResourceRequirements
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
                                Env = new List<V1EnvVar>
                                {
                                    new V1EnvVar
                                    {
                                        Name = "ACCEPT_EULA",
                                        Value = "Y"
                                    },
                                    new V1EnvVar
                                    {
                                        Name = "SA_PASSWORD",
                                        Value = server.AdminPassword // TODO: Use Secret resource instead.
                                    }
                                },
                                Ports = new List<V1ContainerPort>
                                {
                                    new V1ContainerPort
                                    {
                                        ContainerPort = 1433
                                    }
                                },
                                VolumeMounts = new List<V1VolumeMount>
                                {
                                    new V1VolumeMount
                                    {
                                        Name = "sql-data",
                                        SubPath = baseName,
                                        MountPath = "/var/opt/mssql"
                                    }
                                }
                            },
                            // Prometheus exporter
                            new V1Container
                            {
                                Name = "prometheus-exporter",
                                Image = "awaragi/prometheus-mssql-exporter:v0.4.1",
                                Env = new List<V1EnvVar>
                                {
                                    new V1EnvVar
                                    {
                                        Name = "SERVER",
                                        Value = "127.0.0.1",
                                    },
                                    new V1EnvVar
                                    {
                                        Name = "USERNAME",
                                        Value = "sa" // TODO: Use Secret resource instead.
                                    },
                                    new V1EnvVar
                                    {
                                        Name = "PASSWORD",
                                        Value = server.AdminPassword // TODO: Use Secret resource instead.
                                    },
                                    new V1EnvVar
                                    {
                                        Name = "DEBUG",
                                        Value = "app"
                                    }
                                },
                                Ports = new List<V1ContainerPort>
                                {
                                    new V1ContainerPort
                                    {
                                        ContainerPort = 4000
                                    }
                                }
                            }
                        },
                        Volumes = new List<V1Volume>
                        {
                            new V1Volume
                            {
                                Name = "sql-data",
                                PersistentVolumeClaim = new V1PersistentVolumeClaimVolumeSource
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
        ///     Build an internally-facing <see cref="V1ServiceSpec"/> for the specified server.
        /// </summary>
        /// <param name="server">
        ///     A <see cref="DatabaseServer"/> representing the target server.
        /// </param>
        /// <returns>
        ///     The configured <see cref="V1ServiceSpec"/>.
        /// </returns>
        public static V1ServiceSpec InternalService(DatabaseServer server)
        {
            if (server == null)
                throw new ArgumentNullException(nameof(server));

            string baseName = KubeResources.GetBaseName(server);

            return new V1ServiceSpec
            {
                Ports = new List<V1ServicePort>
                {
                    new V1ServicePort
                    {
                        Name = "sql-server",
                        Port = 1433,
                        Protocol = "TCP"
                    },
                    new V1ServicePort
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
        ///     Build an externally-facing <see cref="V1ServiceSpec"/> for the specified server.
        /// </summary>
        /// <param name="server">
        ///     A <see cref="DatabaseServer"/> representing the target server.
        /// </param>
        /// <returns>
        ///     The configured <see cref="V1ServiceSpec"/>.
        /// </returns>
        public static V1ServiceSpec ExternalService(DatabaseServer server)
        {
            if (server == null)
                throw new ArgumentNullException(nameof(server));

            string baseName = KubeResources.GetBaseName(server);

            return new V1ServiceSpec
            {
                Type = "NodePort",
                Ports = new List<V1ServicePort>
                {
                    new V1ServicePort
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
    }
}
