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

        /// <summary>
        ///     Build a <see cref="V1Beta1VoyagerIngressSpec"/> for the specified server.
        /// </summary>
        /// <param name="server">
        ///     A <see cref="DatabaseServer"/> representing the target server.
        /// </param>
        /// <returns>
        ///     The configured <see cref="V1Beta1VoyagerIngressSpec"/>.
        /// </returns>
        public static V1Beta1VoyagerIngressSpec Ingress(DatabaseServer server)
        {
            if (server == null)
                throw new ArgumentNullException(nameof(server));

            string baseName = KubeResources.GetBaseName(server);

            return new V1Beta1VoyagerIngressSpec
            {
                Rules = new List<V1Beta1VoyagerIngressRule>
                {
                    new V1Beta1VoyagerIngressRule
                    {
                        Host = $"{server.Name}.local",
                        Tcp = new V1Beta1VoyagerIngressRuleTcp
                        {
                            Port = (11433 + server.Id).ToString(), // Cheaty!
                            Backend = new V1beta1IngressBackend
                            {
                                ServiceName = $"{baseName}-service",
                                ServicePort = "1433"
                            }
                        }
                    }
                }
            };
        }

        /// <summary>
        ///     Build a <see cref="V1JobSpec"/> for executing T-SQL on the specified server.
        /// </summary>
        /// <param name="server">
        ///     A <see cref="DatabaseServer"/> representing the target server.
        /// </param>
        /// <param name="secretName">
        ///     The name of the Secret used to store sensitive scripts and configuration.
        /// </param>
        /// <param name="configMapName">
        ///     The name of the ConfigMap used to store regular scripts and configuration.
        /// </param>
        /// <returns>
        ///     The configured <see cref="V1JobSpec"/>.
        /// </returns>
        public static V1JobSpec ExecuteSql(DatabaseServer server, string secretName, string configMapName)
        {
            if (server == null)
                throw new ArgumentNullException(nameof(server));

            if (String.IsNullOrWhiteSpace(secretName))
                throw new ArgumentException("Argument cannot be null, empty, or entirely composed of whitespace: 'secretName'.", nameof(secretName));
            
            if (String.IsNullOrWhiteSpace(configMapName))
                throw new ArgumentException("Argument cannot be null, empty, or entirely composed of whitespace: 'configMapName'.", nameof(configMapName));
            
            return new V1JobSpec
            {
                Template = new V1PodTemplateSpec
                {
                    Spec = new V1PodSpec
                    {
                        RestartPolicy = "Never",
                        Containers = new List<V1Container>
                        {
                            new V1Container
                            {
                                Name = "sqlcmd",
                                Image = "ddresearch/sql-tools",
                                Env = new List<V1EnvVar>
                                {
                                    new V1EnvVar
                                    {
                                        Name = "SQL_USER",
                                        ValueFrom = new V1EnvVarSource
                                        {
                                            SecretKeyRef = new V1SecretKeySelector
                                            {
                                                Name = secretName,
                                                Key = "database-user"
                                            }
                                        }
                                    },
                                    new V1EnvVar
                                    {
                                        Name = "SQL_PASSWORD",
                                        ValueFrom = new V1EnvVarSource
                                        {
                                            SecretKeyRef = new V1SecretKeySelector
                                            {
                                                Name = secretName,
                                                Key = "database-password"
                                            }
                                        }
                                    },
                                    new V1EnvVar
                                    {
                                        Name = "SQL_HOST",
                                        ValueFrom = new V1EnvVarSource
                                        {
                                            ConfigMapKeyRef = new V1ConfigMapKeySelector
                                            {
                                                Name = configMapName,
                                                Key = "database-server"
                                            }
                                        }
                                    },
                                    new V1EnvVar
                                    {
                                        Name = "SQL_DATABASE",
                                        ValueFrom = new V1EnvVarSource
                                        {
                                            ConfigMapKeyRef = new V1ConfigMapKeySelector
                                            {
                                                Name = configMapName,
                                                Key = "database-name"
                                            }
                                        }
                                    }
                                },
                                VolumeMounts = new List<V1VolumeMount>
                                {
                                    new V1VolumeMount
                                    {
                                        Name = "sql-secrets",
                                        MountPath = "/sql-scripts/secrets"
                                    },
                                    new V1VolumeMount
                                    {
                                        Name = "sql-script",
                                        MountPath = "/sql-scripts/scripts"
                                    }
                                }
                            }
                        },
                        Volumes = new List<V1Volume>
                        {
                            new V1Volume
                            {
                                Name = "sql-secrets",
                                Secret = new V1SecretVolumeSource
                                {
                                    SecretName = secretName
                                }
                            },
                            new V1Volume
                            {
                                Name = "sql-script",
                                ConfigMap = new V1ConfigMapVolumeSource
                                {
                                    Name = configMapName
                                }
                            }
                        }
                    }
                }
            };
        }
    }
}