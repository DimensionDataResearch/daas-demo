using KubeNET.Swagger.Model;
using System;
using System.Collections.Generic;

namespace DaaSDemo.Provisioning
{
    using Data.Models;
    using KubeClient.Models;

    /// <summary>
    ///     Factory methods for common Kubernetes resource specifications.
    /// </summary>
    public static class KubeSpecs
    {
        /// <summary>
        ///     Build a <see cref="V1ReplicationControllerSpec"/> for the specified server.
        /// </summary>
        /// <param name="server">
        ///     A <see cref="DatabaseServer"/> representing the target server.
        /// </param>
        /// <param name="dataVolumeClaimName">
        ///     The name of the Kubernetes VolumeClaim where the data will be stored.
        /// </param>
        /// <returns>
        ///     The configured <see cref="V1ReplicationControllerSpec"/>.
        /// </returns>
        public static V1ReplicationControllerSpec ReplicationController(DatabaseServer server, string dataVolumeClaimName)
        {
            if (server == null)
                throw new ArgumentNullException(nameof(server));

            string baseName = KubeResources.GetBaseName(server);

            return new V1ReplicationControllerSpec
            {
                Replicas = 1,
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
                            new V1Container
                            {
                                Name = baseName,
                                Image = "microsoft/mssql-server-linux:2017-GA",
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
                                    new V1VolumeMountWithSubPath
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
        ///     Build a <see cref="V1ServiceSpec"/> for the specified server.
        /// </summary>
        /// <param name="server">
        ///     A <see cref="DatabaseServer"/> representing the target server.
        /// </param>
        /// <returns>
        ///     The configured <see cref="V1ServiceSpec"/>.
        /// </returns>
        public static V1ServiceSpec Service(DatabaseServer server)
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