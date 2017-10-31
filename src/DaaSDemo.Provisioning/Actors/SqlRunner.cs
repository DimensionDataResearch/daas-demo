using Akka;
using Akka.Actor;
using HTTPlease;
using KubeNET.Swagger.Model;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.Serialization;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace DaaSDemo.Provisioning.Actors
{
    using Data;
    using Data.Models;
    using KubeClient;
    using Messages;

    /// <summary>
    ///     Actor that invokes a Kubernetes job to run SQLCMD.
    /// </summary>
    public class SqlRunner
        : ReceiveActorEx
    {
        /// <summary>
        ///     The <see cref="KubeApiClient"/> used to communicate with the Kubernetes API.
        /// </summary>
        readonly KubeApiClient _kubeClient;

        /// <summary>
        ///     A reference to the actor that owns the <see cref="SqlRunner"/>.
        /// </summary>
        readonly IActorRef _owner;

        /// <summary>
        ///     A <see cref="DatabaseServer"/> representing the target instance of SQL Server.
        /// </summary>
        readonly DatabaseServer _server;

        /// <summary>
        ///     The name of the Kubernetes Secret containing "secrets.sql".
        /// </summary>
        string _secretName;

        /// <summary>
        ///     The name of the Kubernetes ConfigMap containing "scripts.sql".
        /// </summary>
        string _configMapName;

        /// <summary>
        ///     
        /// </summary>
        /// <param name="owner">
        ///     A reference to the actor that owns the <see cref="SqlRunner"/>.
        /// </param>
        /// <param name="serverId">
        ///     The Id of the target instance of SQL Server.
        /// </param>
        public SqlRunner(IActorRef owner, DatabaseServer server)
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));

            if (server == null)
                throw new ArgumentNullException(nameof(server));

            _owner = owner;
            _server = server;
            _secretName = $"sql-exec-{server.Id}-secret";
            _configMapName = $"sql-exec-{server.Id}-config";

            _kubeClient = CreateKubeApiClient();

            ReceiveAsync<ExecuteSql>(Execute);
        }

        /// <summary>
        ///     Execute SQL.
        /// </summary>
        /// <param name="executeSql">
        ///     An <see cref="ExecuteSql"/> representing the T-SQL to execute.
        /// </param>
        /// <returns>
        ///     A <see cref="Task"/> representing the operation.
        /// </returns>
        async Task Execute(ExecuteSql executeSql)
        {
            if (executeSql == null)
                throw new ArgumentNullException(nameof(executeSql));

            V1Service serverService = await FindService();
            if (serverService == null)
            {
                Log.Error("Cannot find Service for server {ServerId}.", _server.Id);

                _owner.Tell(
                    new Status.Failure(new Exception(
                        message: $"Cannot find Service for server {_server.Id}."
                    ))
                );

                Context.Stop(Self);

                return;
            }

            V1Secret secret = await EnsureSecretPresent(
                databaseName: executeSql.DatabaseName,
                databaseUser: "sa",
                databasePassword: _server.AdminPassword
            );

            V1ConfigMap configMap = await EnsureConfigMapPresent(
                service: serverService,
                databaseName: executeSql.DatabaseName,
                sql: executeSql.Sql
            );

            V1JobSpec jobSpec = BuildJobSpec(secret, configMap);

            // TODO: Launch job and use Become to poll for job completion.
        }

        /// <summary>
        ///     Called when the actor has stopped.
        /// </summary>
        protected override void PostStop()
        {
            _kubeClient.Dispose();

            base.PostStop();
        }

        /// <summary>
        ///     Build a job specification for executing the specified T-SQL.
        /// </summary>
        /// <param name="secret">
        ///     The Secret to be used by the job.
        /// </param>
        /// <param name="configMap">
        ///     The ConfigMap to be used by the job.
        /// </param>
        /// <returns>
        ///     The configured <see cref="V1JobSpec"/>.
        /// </returns>
        V1JobSpec BuildJobSpec(V1Secret secret, V1ConfigMap configMap)
        {
            if (secret == null)
                throw new ArgumentNullException(nameof(secret));

            if (configMap == null)
                throw new ArgumentNullException(nameof(configMap));

            return new V1JobSpec
            {
                Template = new V1PodTemplateSpec
                {
                    Spec = new V1PodSpec
                    {
                        Containers = new List<V1Container>
                        {
                            new V1Container
                            {
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
                                                Name = secret.Metadata.Name,
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
                                                Name = secret.Metadata.Name,
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
                                                Name = configMap.Metadata.Name,
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
                                                Name = configMap.Metadata.Name,
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
                                        MountPath = "/sql-scripts/secrets.sql"
                                    },
                                    new V1VolumeMount
                                    {
                                        Name = "sql-script",
                                        MountPath = "/sql-scripts/script.sql"
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
                                    SecretName = secret.Metadata.Name
                                }
                            },
                            new V1Volume
                            {
                                Name = "sql-script",
                                ConfigMap = new V1ConfigMapVolumeSource
                                {
                                    Name = "TODO-CREATE-CONFIG-MAP"
                                }
                            }
                        }
                    }
                }
            };
        }

        /// <summary>
        ///     Find the server's current Job (if any) for executing T-SQL.
        /// </summary>
        /// <returns>
        ///     The Job, or <c>null</c> if it was not found.
        /// </returns>
        async Task<V1Job> FindJob()
        {
            List<V1Job> matchingJobs =
                await _kubeClient.JobsV1.List(
                    labelSelector: $"cloud.dimensiondata.daas.server-id = {_server.Id}, cloud.dimensiondata.daas.action = exec-sql"
                );

            if (matchingJobs.Count == 0)
                return null;

            return matchingJobs[matchingJobs.Count - 1];
        }

        /// <summary>
        ///     Find the secret used to execute T-SQL in the specified database.
        /// </summary>
        /// <param name="databaseName">
        ///     The database name.
        /// </param>
        /// <returns>
        ///     A <see cref="V1Secret"/> representing the secret, or <c>null</c>, if the secret was not found.
        /// </returns>
        async Task<V1Secret> FindSecret(string databaseName)
        {
            if (String.IsNullOrWhiteSpace(databaseName))
                throw new ArgumentException("Argument cannot be null, empty, or entirely composed of whitespace: 'databaseName'.", nameof(databaseName));

            string labelSelector = $"cloud.dimensiondata.daas.server-id = {_server.Id}, cloud.dimensiondata.daas.database = {databaseName}, cloud.dimensiondata.daas.action = exec-sql";

            List<V1Secret> matchingSecrets =
                await _kubeClient.SecretsV1.List(
                    labelSelector: $"cloud.dimensiondata.daas.server-id = {_server.Id}, cloud.dimensiondata.daas.database = {databaseName}, cloud.dimensiondata.daas.action = exec-sql"
                );

            if (matchingSecrets.Count == 0)
                return null;

            return matchingSecrets[matchingSecrets.Count - 1]; // Most recent.
        }

        /// <summary>
        ///     Ensure that the Kubernetes Secret exists for executing T-SQL in the specified database (creating it if necessary).
        /// </summary>
        /// <param name="databaseName">
        ///     The name of the target database.
        /// </param>
        /// <param name="databaseUser">
        ///     The database user name.
        /// </param>
        /// <param name="databasePassword">
        ///     The database password.
        /// </param>
        /// <returns>
        ///     <c>true</c>, if the secret exists; otherwise, <c>false</c>.
        /// </returns>
        async Task<V1Secret> EnsureSecretPresent(string databaseName, string databaseUser, string databasePassword)
        {
            if (String.IsNullOrWhiteSpace(databaseName))
                throw new ArgumentException("Argument cannot be null, empty, or entirely composed of whitespace: 'databaseName'.", nameof(databaseName));

            if (String.IsNullOrWhiteSpace(databaseUser))
                throw new ArgumentException("Argument cannot be null, empty, or entirely composed of whitespace: 'databaseUser'.", nameof(databaseUser));

            if (String.IsNullOrWhiteSpace(databasePassword))
                throw new ArgumentException("Argument cannot be null, empty, or entirely composed of whitespace: 'databasePassword'.", nameof(databasePassword));

            V1Secret existingSecret = await FindSecret(databaseName);
            if (existingSecret != null)
            {
                Log.Info("Found existing secret {SecretName} for database {DatabaseName} in server {ServerId}.",
                    existingSecret.Metadata.Name,
                    databaseName,
                    _server.Id
                );

                return existingSecret;
            }

            var newSecret = new V1Secret
            {
                ApiVersion = "v1",
                Kind = "secret",
                Metadata = new V1ObjectMeta
                {
                    Name = $"{_secretName}-{databaseName}",
                    Labels = new Dictionary<string, string>
                    {
                        ["cloud.dimensiondata.daas.server-id"] = _server.Id.ToString(),
                        ["cloud.dimensiondata.daas.database"] = databaseName,
                        ["cloud.dimensiondata.daas.action"] = "exec-sql"
                    }
                },
                Type = "Opaque",
            };
            newSecret.AddData("database-user", databaseUser);
            newSecret.AddData("database-password", databasePassword);
            newSecret.AddData("secrets.sql", $@"
                :setvar DatabaseName '{databaseName}'
                :setvar DatabaseUser '{databaseUser}'
                :setvar DatabasePassword '{databasePassword}'
            ");

            V1Secret createdSecret = await _kubeClient.SecretsV1.Create(newSecret);

            Log.Info("Successfully created secret {SecretName} for database {DatabaseName} in server {ServerId}.",
                createdSecret.Metadata.Name,
                databaseName,
                _server.Id
            );

            return createdSecret;
        }

        /// <summary>
        ///     Ensure that the Kubernetes Secret does not exist for executing T-SQL in the specified database (deleting it if necessary).
        /// </summary>
        /// <param name="databaseName">
        ///     The name of the target database.
        /// </param>
        /// <returns>
        ///     <c>true</c>, if the secret does not exist; otherwise, <c>false</c>.
        /// </returns>
        async Task<bool> EnsureSecretAbsent(string databaseName)
        {
            V1Secret secret = await FindSecret(databaseName);
            if (secret == null)
                return true;

            Log.Info("Deleting secret {SecretName} for database {DatabaseName} in server {ServerId}...",
                secret.Metadata.Name,
                databaseName,
                _server.Id
            );

            try
            {
                await _kubeClient.SecretsV1.Delete(
                    name: secret.Metadata.Name
                );
            }
            catch (HttpRequestException<UnversionedStatus> deleteFailed)
            {
                Log.Error("Failed to delete secret {SecretName} for database {DatabaseName} in server {ServerId} (Message:{FailureMessage}, Reason:{FailureReason}).",
                    secret.Metadata.Name,
                    databaseName,
                    _server.Id,
                    deleteFailed.Response.Message,
                    deleteFailed.Response.Reason
                );

                return false;
            }

            Log.Info("Deleted secret {SecretName} for database {DatabaseName} in server {ServerId}.",
                secret.Metadata.Name,
                databaseName,
                _server.Id
            );

            return true;
        }

        /// <summary>
        ///     Find the ConfigMap used to execute T-SQL in the specified database.
        /// </summary>
        /// <param name="databaseName">
        ///     The database name.
        /// </param>
        /// <returns>
        ///     A <see cref="V1ConfigMap"/> representing the ConfigMap, or <c>null</c>, if the ConfigMap was not found.
        /// </returns>
        async Task<V1ConfigMap> FindConfigMap(string databaseName)
        {
            if (String.IsNullOrWhiteSpace(databaseName))
                throw new ArgumentException("Argument cannot be null, empty, or entirely composed of whitespace: 'databaseName'.", nameof(databaseName));

            List<V1ConfigMap> matchingConfigMaps = await _kubeClient.ConfigMapsV1.List(
                kubeNamespace: "default",
                labelSelector: $"cloud.dimensiondata.daas.server-id = {_server.Id}, cloud.dimensiondata.daas.database = {databaseName}, cloud.dimensiondata.daas.action = exec-sql"
            );

            if (matchingConfigMaps.Count == 0)
                return null;

            return matchingConfigMaps[matchingConfigMaps.Count - 1]; // Most recent.
        }

        /// <summary>
        ///     Ensure that the Kubernetes ConfigMap exists for executing T-SQL in the specified database (creating it if necessary).
        /// </summary>
        /// <param name="service">
        ///     The Service used to communicate with the target instance of SQL Server.
        /// </param>
        /// <param name="databaseName">
        ///     The name of the target database.
        /// </param>
        /// <param name="sql">
        ///     The T-SQL to be executed.
        /// </param>
        /// <returns>
        ///     <c>true</c>, if the ConfigMap exists; otherwise, <c>false</c>.
        /// </returns>
        async Task<V1ConfigMap> EnsureConfigMapPresent(V1Service service, string databaseName, string sql)
        {
            if (service == null)
                throw new ArgumentNullException(nameof(service));

            if (String.IsNullOrWhiteSpace(databaseName))
                throw new ArgumentException("Argument cannot be null, empty, or entirely composed of whitespace: 'databaseName'.", nameof(databaseName));

            if (String.IsNullOrWhiteSpace(sql))
                throw new ArgumentException("Argument cannot be null, empty, or entirely composed of whitespace: 'sql'.", nameof(sql));

            V1ConfigMap existingConfigMap = await FindConfigMap(databaseName);
            if (existingConfigMap != null)
            {
                Log.Info("Found existing ConfigMap {ConfigMapName} for database {DatabaseName} in server {ServerId}.",
                    existingConfigMap.Metadata.Name,
                    databaseName,
                    _server.Id
                );

                return existingConfigMap;
            }

            var newConfigMap = new V1ConfigMap
            {
                ApiVersion = "v1",
                Kind = "configMap",
                Metadata = new V1ObjectMeta
                {
                    Name = $"{_configMapName}-{databaseName}",
                    Labels = new Dictionary<string, string>
                    {
                        ["cloud.dimensiondata.daas.server-id"] = _server.Id.ToString(),
                        ["cloud.dimensiondata.daas.database"] = databaseName,
                        ["cloud.dimensiondata.daas.action"] = "exec-sql"
                    }
                }
            };
            newConfigMap.AddData("database-server",
                value: $"{service.Metadata.Name}.{service.Metadata.Namespace}.svc.cluster.local,{service.Spec.Ports[0].Port}"
            );
            newConfigMap.AddData("database-name",
                value: databaseName
            );
            newConfigMap.AddData("script.sql",
                value: sql
            );

            V1ConfigMap createdConfigMap = await _kubeClient.ConfigMapsV1.Create(newConfigMap);

            Log.Info("Successfully created ConfigMap {ConfigMapName} for database {DatabaseName} in server {ServerId}.",
                createdConfigMap.Metadata.Name,
                databaseName,
                _server.Id
            );

            return createdConfigMap;
        }

        /// <summary>
        ///     Ensure that the Kubernetes ConfigMap does not exist for executing T-SQL in the specified database (deleting it if necessary).
        /// </summary>
        /// <param name="databaseName">
        ///     The name of the target database.
        /// </param>
        /// <returns>
        ///     <c>true</c>, if the ConfigMap does not exist; otherwise, <c>false</c>.
        /// </returns>
        async Task<bool> EnsureConfigMapAbsent(string databaseName)
        {
            V1ConfigMap configMap = await FindConfigMap(databaseName);
            if (configMap == null)
                return true;

            Log.Info("Deleting ConfigMap {ConfigMapName} for database {DatabaseName} in server {ServerId}...",
                configMap.Metadata.Name,
                databaseName,
                _server.Id
            );

            try
            {
                await _kubeClient.ConfigMapsV1.Delete(
                    name: configMap.Metadata.Name
                );
            }
            catch (HttpRequestException<UnversionedStatus> deleteFailed)
            {
                Log.Error("Failed to delete ConfigMap {ConfigMapName} for database {DatabaseName} in server {ServerId} (Message:{FailureMessage}, Reason:{FailureReason}).",
                    configMap.Metadata.Name,
                    databaseName,
                    _server.Id,
                    deleteFailed.Response.Message,
                    deleteFailed.Response.Reason
                );

                return false;
            }

            Log.Info("Deleted ConfigMap {ConfigMapName} for database {DatabaseName} in server {ServerId}.",
                configMap.Metadata.Name,
                databaseName,
                _server.Id
            );

            return true;
        }

        /// <summary>
        ///     Find the server's associated Service (if it exists).
        /// </summary>
        /// <returns>
        ///     The Service, or <c>null</c> if it was not found.
        /// </returns>
        async Task<V1Service> FindService()
        {
            List<V1Service> matchingServices = await _kubeClient.ServicesV1.List(
                kubeNamespace: "default",
                labelSelector: $"cloud.dimensiondata.daas.server-id = {_server.Id}"
            );

            return matchingServices[matchingServices.Count - 1];
        }

        /// <summary>
        ///     Create a new <see cref="KubeApiClient"/> for communicating with the Kubernetes API.
        /// </summary>
        /// <returns>
        ///     The configured <see cref="KubeApiClient"/>.
        /// </returns>
        KubeApiClient CreateKubeApiClient()
        {
            return KubeApiClient.Create(
                endPointUri: new Uri(
                    Context.System.Settings.Config.GetString("daas.kube.api-endpoint")
                ),
                accessToken: Context.System.Settings.Config.GetString("daas.kube.api-token")
            );
        }
    }

    /// <summary>
    ///     Message requesting execution of T-SQL.
    /// </summary>
    public class ExecuteSql
    {
        /// <summary>
        ///     Create a new <see cref="ExecuteSql"/> message.
        /// </summary>
        /// <param name="databaseName">
        ///     The name of the target database where the T-SQL will be executed.
        /// </param>
        /// <param name="sql">
        ///     The T-SQL to execute.
        /// </param>
        public ExecuteSql(string databaseName, string sql)
        {
            if (String.IsNullOrWhiteSpace(sql))
                throw new ArgumentException("Argument cannot be null, empty, or entirely composed of whitespace: 'sql'.", nameof(sql));

            DatabaseName = databaseName;
            Sql = sql;
        }

        /// <summary>
        ///     The name of the target database where the T-SQL will be executed.
        /// </summary>
        public string DatabaseName { get; }

        /// <summary>
        ///     The T-SQL to execute.
        /// </summary>
        public string Sql { get; }
    }

    /// <summary>
    ///     Extension methods for Kubernetes model types.
    /// </summary>
    public static class KubeModelExtensions
    {
        /// <summary>
        ///     The default encoding (ASCII) used by these Kubernetes model extensions.
        /// </summary>
        public static readonly Encoding DefaultEncoding = Encoding.ASCII;

        /// <summary>
        ///     Add data to a ConfigMap.
        /// </summary>
        /// <param name="configMap">
        ///     The ConfigMap.
        /// </param>
        /// <param name="name">
        ///     The name of the data to add.
        /// </param>
        /// <param name="value">
        ///     The value to add.
        /// </param>
        /// <param name="encoding">
        ///     An optional encoding to use (defaults to <see cref="DefaultEncoding"/>).
        /// </param>
        /// <returns>
        ///     The ConfigMap (enables inline use / method-chaining).
        /// </returns>
        public static V1ConfigMap AddData(this V1ConfigMap configMap, string name, string value, Encoding encoding = null)
        {
            if (configMap == null)
                throw new ArgumentNullException(nameof(configMap));

            if (String.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Argument cannot be null, empty, or entirely composed of whitespace: 'name'.", nameof(name));

            if (value == null)
                throw new ArgumentNullException(nameof(value));

            if (configMap.Data == null)
                configMap.Data = new Dictionary<string, string>();

            configMap.Data.Add(name, Convert.ToBase64String(
                (encoding ?? DefaultEncoding).GetBytes(value)
            ));

            return configMap;
        }

        /// <summary>
        ///     Add data to a Secret.
        /// </summary>
        /// <param name="secret">
        ///     The Secret.
        /// </param>
        /// <param name="name">
        ///     The name of the data to add.
        /// </param>
        /// <param name="value">
        ///     The value to add.
        /// </param>
        /// <param name="encoding">
        ///     An optional encoding to use (defaults to <see cref="DefaultEncoding"/>).
        /// </param>
        /// <returns>
        ///     The Secret (enables inline use / method-chaining).
        /// </returns>
        public static V1Secret AddData(this V1Secret secret, string name, string value, Encoding encoding = null)
        {
            if (secret == null)
                throw new ArgumentNullException(nameof(secret));

            if (String.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Argument cannot be null, empty, or entirely composed of whitespace: 'name'.", nameof(name));

            if (value == null)
                throw new ArgumentNullException(nameof(value));

            if (secret.Data == null)
                secret.Data = new Dictionary<string, string>();

            secret.Data.Add(name, Convert.ToBase64String(
                (encoding ?? DefaultEncoding).GetBytes(value)
            ));

            return secret;
        }
    }
}
