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
        ///     The maximum amount of time to wait for a job to complete.
        /// </summary>
        public static readonly TimeSpan JobCompletionTimeout = TimeSpan.FromMinutes(2);

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

        V1Job _existingJob;

        ICancelable _timerCancellation;

        ICancelable _timeoutCancellation;

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
            _kubeClient = CreateKubeApiClient();

            Become(Ready);
        }

        void Ready()
        {
            if (_timeoutCancellation != null)
            {
                _timeoutCancellation.Cancel();
                _timeoutCancellation = null;
            }

            if (_timerCancellation != null)
            {
                _timerCancellation.Cancel();
                _timerCancellation = null;
            }

            ReceiveAsync<ExecuteSql>(Execute);
        }

        void WaitForExistingJob()
        {
            _timerCancellation = Context.System.Scheduler.ScheduleTellRepeatedlyCancelable(
                initialDelay: TimeSpan.FromSeconds(5),
                interval: TimeSpan.FromSeconds(5),
                receiver: Self,
                message: Command.PollJobStatus,
                sender: Self
            );

            _timeoutCancellation = Context.System.Scheduler.ScheduleTellOnceCancelable(
                delay: JobCompletionTimeout,
                receiver: Self,
                message: Command.Timeout,
                sender: Self
            );

            ReceiveAsync<Command>(async command =>
            {
                switch (command)
                {
                    case Command.PollJobStatus:
                    {
                        string jobName = _existingJob.Metadata.Name;
                        
                        _existingJob = await _kubeClient.JobsV1.GetByName(jobName);
                        if (_existingJob == null)
                        {
                            Log.Info("Job {JobName} not found; will treat as completed.", jobName);

                            Become(Ready);
                        }

                        if (_existingJob.Status.Active == 0)
                        {
                            if (_existingJob.Status.Conditions[0].Type == "Complete")
                            {
                                Log.Info("Job {JobName} has successfully completed.",
                                    _existingJob.Metadata.Name
                                );
                            }
                            else
                            {
                                Log.Info("Job {JobName} failed ({FailureReason}:{FailureMessage}).",
                                    _existingJob.Metadata.Name,
                                    _existingJob.Status.Conditions[0].Reason,
                                    _existingJob.Status.Conditions[0].Message                                    
                                );
                            }

                            Become(Ready);
                        }

                        break;
                    }
                    case Command.Timeout:
                    {
                        string jobName = _existingJob.Metadata.Name;

                        Log.Info("Timed out after waiting {JobCompletionTimeout} for Job {JobName} to complete.", JobCompletionTimeout, jobName);
                        
                        _existingJob = await _kubeClient.JobsV1.GetByName(jobName);
                        if (_existingJob != null)
                        {
                            Log.Info("Deleting Job {JobName}...", jobName);

                            await _kubeClient.JobsV1.Delete(jobName);
                            _existingJob = null;

                            Log.Info("Deleted Job {JobName}.", jobName);
                        }
                        else
                            Log.Info("Job {JobName} not found; will treat as completed.", jobName);

                        Become(Ready);

                        break;
                    }
                    default:
                    {
                        Unhandled(command);

                        break;
                    }
                }
            });
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

            V1Service serverService = await FindServerService();
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

            _existingJob = await FindJob(executeSql);
            if (_existingJob != null)
            {
                Log.Info("Found existing job {JobName}.", _existingJob.Metadata.Name);

                // TODO: Do conditions only show up when job is complete, or are they *always* present?
                if (_existingJob.Status.Active  == 0)
                {
                    Log.Info("Deleting existing job {JobName}...", _existingJob.Metadata.Name);

                    await _kubeClient.JobsV1.Delete(
                        _existingJob.Metadata.Name
                    );

                    Log.Info("Deleted existing job {JobName}.", _existingJob.Metadata.Name);
                }
                else
                {
                    Log.Info("Existing job {JobName} still has {ActivePodCount} active pods; will wait {JobCompletionTimeout} before forcing job termination...",
                        JobCompletionTimeout,
                        _existingJob.Metadata.Name,
                        _existingJob.Status.Active
                    );

                    // Existing job is running; wait for it to terminate.
                    Become(WaitForExistingJob);

                    return;
                }
            }

            V1Secret secret = await EnsureSecretPresent(executeSql);

            V1ConfigMap configMap = await EnsureConfigMapPresent(executeSql, serverService);

            try
            {
                await _kubeClient.JobsV1.Create(new V1Job
                {
                    ApiVersion = "batch/v1",
                    Kind = "Job",
                    Metadata = new V1ObjectMeta
                    {
                        Name = GetJobName(executeSql),
                        Labels = new Dictionary<string, string>
                        {
                            ["cloud.dimensiondata.daas.server-id"] = _server.Id.ToString(),
                            ["cloud.dimensiondata.daas.database"] = executeSql.DatabaseName
                        }
                    },
                    Spec = KubeSpecs.ExecuteSql(_server,
                        secretName: secret.Metadata.Name,
                        configMapName: configMap.Metadata.Name
                    )
                });
            }
            catch (HttpRequestException<UnversionedStatus> createFailed)
            {
                Log.Error("Failed to create Job {JobName} for database {DatabaseName} in server {ServerId} (Message:{FailureMessage}, Reason:{FailureReason}).",
                    $"sqlcmd-{_server.Id}-{executeSql.DatabaseName}",
                    executeSql.DatabaseName,
                    _server.Id,
                    createFailed.Response.Message,
                    createFailed.Response.Reason
                );

                _owner.Tell(
                    new Status.Failure(createFailed)
                );

                Context.Stop(Self);
            }
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
        ///     Find the server's current Job (if any) for executing T-SQL.
        /// </summary>
        /// <param name="executeSql">
        ///     The <see cref="ExecuteSql"/> message representing the T-SQL to be executed.
        /// </param>
        /// <returns>
        ///     The Job, or <c>null</c> if it was not found.
        /// </returns>
        async Task<V1Job> FindJob(ExecuteSql executeSql)
        {
            return await _kubeClient.JobsV1.GetByName(
                name: GetJobName(executeSql)
            );
        }

        /// <summary>
        ///     Find the secret used to execute T-SQL in the specified database.
        /// </summary>
        /// <param name="executeSql">
        ///     An <see cref="ExecuteSql"/> message representing the T-SQL to execute.
        /// </param>
        /// <returns>
        ///     A <see cref="V1Secret"/> representing the secret, or <c>null</c>, if the secret was not found.
        /// </returns>
        async Task<V1Secret> FindSecret(ExecuteSql executeSql)
        {
            if (executeSql == null)
                throw new ArgumentNullException(nameof(executeSql));

            return await _kubeClient.SecretsV1.GetByName(
                KubeResources.GetJobName(executeSql, _server)
            );
        }

        /// <summary>
        ///     Ensure that the Kubernetes Secret exists for executing the specified T-SQL (creating it if necessary).
        /// </summary>
        /// <param name="executeSql">
        ///     An <see cref="ExecuteSql"/> message representing the T-SQL to execute.
        /// </param>
        /// <returns>
        ///     <c>true</c>, if the secret exists; otherwise, <c>false</c>.
        /// </returns>
        async Task<V1Secret> EnsureSecretPresent(ExecuteSql executeSql)
        {
            if (executeSql == null)
                throw new ArgumentNullException(nameof(executeSql));

            V1Secret existingSecret = await FindSecret(executeSql);
            if (existingSecret != null)
            {
                Log.Info("Found existing secret {SecretName} for database {DatabaseName} in server {ServerId}.",
                    existingSecret.Metadata.Name,
                    executeSql.DatabaseName,
                    _server.Id
                );

                return existingSecret;
            }

            var newSecret = new V1Secret
            {
                ApiVersion = "v1",
                Kind = "Secret",
                Metadata = new V1ObjectMeta
                {
                    Name = KubeResources.GetJobName(executeSql, _server),
                    Labels = new Dictionary<string, string>
                    {
                        ["cloud.dimensiondata.daas.server-id"] = _server.Id.ToString(),
                        ["cloud.dimensiondata.daas.database"] = executeSql.DatabaseName,
                        ["cloud.dimensiondata.daas.action"] = "exec-sql"
                    }
                },
                Type = "Opaque",
                Data = new Dictionary<string, string>()
            };
            newSecret.AddData("database-user", "sa");
            newSecret.AddData("database-password", _server.AdminPassword);
            newSecret.AddData("secrets.sql", $@"
                :setvar DatabaseName '{executeSql.DatabaseName}'
                :setvar DatabaseUser 'sa'
                :setvar DatabasePassword '{_server.AdminPassword}'
            ");

            V1Secret createdSecret;
            try
            {
                createdSecret = await _kubeClient.SecretsV1.Create(newSecret);
            }
            catch (HttpRequestException<UnversionedStatus> createFailed)
            {
                Log.Error("Failed to create Secret {SecretName} for database {DatabaseName} in server {ServerId} (Message:{FailureMessage}, Reason:{FailureReason}).",
                    newSecret.Metadata.Name,
                    executeSql.DatabaseName,
                    _server.Id,
                    createFailed.Response.Message,
                    createFailed.Response.Reason
                );

                throw;
            }

            Log.Info("Successfully created secret {SecretName} for database {DatabaseName} in server {ServerId}.",
                createdSecret.Metadata.Name,
                executeSql.DatabaseName,
                _server.Id
            );

            return createdSecret;
        }

        /// <summary>
        ///     Ensure that the Kubernetes Secret does not exist for executing T-SQL in the specified database (deleting it if necessary).
        /// </summary>
        /// <param name="executeSql">
        ///     An <see cref="ExecuteSql"/> message representing the T-SQL to execute.
        /// </param>
        /// <returns>
        ///     <c>true</c>, if the secret does not exist; otherwise, <c>false</c>.
        /// </returns>
        async Task<bool> EnsureSecretAbsent(ExecuteSql executeSql)
        {
            if (executeSql == null)
                throw new ArgumentNullException(nameof(executeSql));
            
            V1Secret secret = await FindSecret(executeSql);
            if (secret == null)
                return true;

            Log.Info("Deleting secret {SecretName} for database {DatabaseName} in server {ServerId}...",
                secret.Metadata.Name,
                executeSql.DatabaseName,
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
                    executeSql.DatabaseName,
                    _server.Id,
                    deleteFailed.Response.Message,
                    deleteFailed.Response.Reason
                );

                return false;
            }

            Log.Info("Deleted secret {SecretName} for database {DatabaseName} in server {ServerId}.",
                secret.Metadata.Name,
                executeSql.DatabaseName,
                _server.Id
            );

            return true;
        }

        /// <summary>
        ///     Find the ConfigMap used to execute T-SQL in the specified database.
        /// </summary>
        /// <param name="executeSql">
        ///     An <see cref="ExecuteSql"/> message representing the T-SQL to execute.
        /// </param>
        /// <returns>
        ///     A <see cref="V1ConfigMap"/> representing the ConfigMap, or <c>null</c>, if the ConfigMap was not found.
        /// </returns>
        async Task<V1ConfigMap> FindConfigMap(ExecuteSql executeSql)
        {
            if (executeSql == null)
                throw new ArgumentNullException(nameof(executeSql));

            return await _kubeClient.ConfigMapsV1.GetByName(
                name: KubeResources.GetJobName(executeSql, _server)
            );
        }

        /// <summary>
        ///     Ensure that the Kubernetes ConfigMap exists for executing T-SQL in the specified database (creating it if necessary).
        /// </summary>
        /// <param name="executeSql">
        ///     An <see cref="ExecuteSql"/> message representing the T-SQL to execute.
        /// </param>
        /// <param name="serverService">
        ///     The Service used to communicate with the target instance of SQL Server.
        /// </param>
        /// <returns>
        ///     <c>true</c>, if the ConfigMap exists; otherwise, <c>false</c>.
        /// </returns>
        async Task<V1ConfigMap> EnsureConfigMapPresent(ExecuteSql executeSql, V1Service serverService)
        {
            if (executeSql == null)
                throw new ArgumentNullException(nameof(executeSql));

            if (serverService == null)
                throw new ArgumentNullException(nameof(serverService));

            V1ConfigMap existingConfigMap = await FindConfigMap(executeSql);
            if (existingConfigMap != null)
            {
                Log.Info("Found existing ConfigMap {ConfigMapName} for database {DatabaseName} in server {ServerId}.",
                    existingConfigMap.Metadata.Name,
                    executeSql.DatabaseName,
                    _server.Id
                );

                return existingConfigMap;
            }

            var newConfigMap = new V1ConfigMap
            {
                ApiVersion = "v1",
                Kind = "ConfigMap",
                Metadata = new V1ObjectMeta
                {
                    Name = KubeResources.GetJobName(executeSql, _server),
                    Labels = new Dictionary<string, string>
                    {
                        ["cloud.dimensiondata.daas.server-id"] = _server.Id.ToString(),
                        ["cloud.dimensiondata.daas.database"] = executeSql.DatabaseName,
                        ["cloud.dimensiondata.daas.action"] = "exec-sql"
                    }
                },
                Data = new Dictionary<string, string>()
            };
            newConfigMap.AddData("database-server",
                value: $"{serverService.Metadata.Name}.{serverService.Metadata.Namespace}.svc.cluster.local,{serverService.Spec.Ports[0].Port}"
            );
            newConfigMap.AddData("database-name",
                value: executeSql.DatabaseName
            );
            newConfigMap.AddData("script.sql",
                value: executeSql.Sql
            );

            V1ConfigMap createdConfigMap;
            try
            {
                createdConfigMap = await _kubeClient.ConfigMapsV1.Create(newConfigMap);
            }
            catch (HttpRequestException<UnversionedStatus> createFailed)
            {
                Log.Error("Failed to create ConfigMap {ConfigMapName} for database {DatabaseName} in server {ServerId} (Message:{FailureMessage}, Reason:{FailureReason}).",
                    newConfigMap.Metadata.Name,
                    executeSql.DatabaseName,
                    _server.Id,
                    createFailed.Response.Message,
                    createFailed.Response.Reason
                );

                throw;
            }

            Log.Info("Successfully created ConfigMap {ConfigMapName} for database {DatabaseName} in server {ServerId}.",
                createdConfigMap.Metadata.Name,
                executeSql.DatabaseName,
                _server.Id
            );

            return createdConfigMap;
        }

        /// <summary>
        ///     Ensure that the Kubernetes ConfigMap does not exist for executing T-SQL in the specified database (deleting it if necessary).
        /// </summary>
        /// <param name="executeSql">
        ///     An <see cref="ExecuteSql"/> message representing the T-SQL to execute.
        /// </param>
        /// <returns>
        ///     <c>true</c>, if the ConfigMap does not exist; otherwise, <c>false</c>.
        /// </returns>
        async Task<bool> EnsureConfigMapAbsent(ExecuteSql executeSql)
        {
            if (executeSql == null)
                throw new ArgumentNullException(nameof(executeSql));

            V1ConfigMap configMap = await FindConfigMap(executeSql);
            if (configMap == null)
                return true;

            Log.Info("Deleting ConfigMap {ConfigMapName} for database {DatabaseName} in server {ServerId}...",
                configMap.Metadata.Name,
                executeSql.DatabaseName,
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
                    executeSql.DatabaseName,
                    _server.Id,
                    deleteFailed.Response.Message,
                    deleteFailed.Response.Reason
                );

                return false;
            }

            Log.Info("Deleted ConfigMap {ConfigMapName} for database {DatabaseName} in server {ServerId}.",
                configMap.Metadata.Name,
                executeSql.DatabaseName,
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
        async Task<V1Service> FindServerService()
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

        /// <summary>
        ///     Get the name of the job used to execute T-SQL.
        /// </summary>
        /// <param name="executeSql">
        ///     An <see cref="ExecuteSql"/> message representing the T-SQL to execute.
        /// </param>
        /// <returns>
        ///     The job name.
        /// </returns>
        string GetJobName(ExecuteSql executeSql) => $"sqlcmd-{_server.Id}-{executeSql.DatabaseName}-{executeSql.JobName}";

        /// <summary>
        ///     Well-known commands understood by the <see cref="SqlRunner"/> actor.
        /// </summary>
        enum Command
        {
            /// <summary>
            ///     Poll the status of an existing job.
            /// </summary>
            PollJobStatus,

            /// <summary>
            ///     Terminate the polling of an existing job's status due to timeout.
            /// </summary>
            Timeout
        }
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
        /// <returns>
        ///     The ConfigMap (enables inline use / method-chaining).
        /// </returns>
        public static V1ConfigMap AddData(this V1ConfigMap configMap, string name, string value)
        {
            if (configMap == null)
                throw new ArgumentNullException(nameof(configMap));

            if (String.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Argument cannot be null, empty, or entirely composed of whitespace: 'name'.", nameof(name));

            if (value == null)
                throw new ArgumentNullException(nameof(value));

            if (configMap.Data == null)
                configMap.Data = new Dictionary<string, string>();

            configMap.Data.Add(name, value);

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
