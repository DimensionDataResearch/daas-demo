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
        : ReceiveActorEx, IWithUnboundedStash
    {
        /// <summary>
        ///     The maximum amount of time to wait for a job to complete.
        /// </summary>
        public static readonly TimeSpan JobCompletionTimeout = TimeSpan.FromMinutes(2);

        /// <summary>
        ///     Cancellation for the periodic poll signal.
        /// </summary>
        ICancelable _pollCancellation;

        /// <summary>
        ///     Cancellation for the timeout signal.
        /// </summary>
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

            Owner = owner;
            Server = server;
            KubeClient = CreateKubeApiClient();
        }

        /// <summary>
        ///     The actor's local message-stash facility.
        /// </summary>
        public IStash Stash { get; set; }

        /// <summary>
        ///     A reference to the actor that owns the <see cref="SqlRunner"/>.
        /// </summary>
        IActorRef Owner { get; }

        /// <summary>
        ///     A <see cref="DatabaseServer"/> representing the target instance of SQL Server.
        /// </summary>
        DatabaseServer Server { get; }

        /// <summary>
        ///     The <see cref="KubeApiClient"/> used to communicate with the Kubernetes API.
        /// </summary>
        KubeApiClient KubeClient { get; }

        /// <summary>
        ///     An <see cref="ExecuteSql"/> representing the current request.
        /// </summary>
        ExecuteSql CurrentRequest { get; set; }

        /// <summary>
        ///     The current state of the Job (if any) used to execute T-SQL.
        /// </summary>
        V1Job Job { get; set; }

        /// <summary>
        ///     Called when the actor is started.
        /// </summary>
        protected override void PreStart()
        {
            Become(Ready);
        }

        /// <summary>
        ///     Called when the actor has stopped.
        /// </summary>
        protected override void PostStop()
        {
            KubeClient.Dispose();

            base.PostStop();
        }

        /// <summary>
        ///     Called when the actor is ready to process requests.
        /// </summary>
        void Ready()
        {
            StopPolling();
            Stash.UnstashAll();

            CurrentRequest = null;
            Job = null;

            ReceiveAsync<ExecuteSql>(async executeSql =>
            {
                CurrentRequest = executeSql;
                
                await Execute();
            });
        }

        /// <summary>
        ///     Called when the actor is executing a newly-created job.
        /// </summary>
        void ExecutingJob()
        {
            StartPolling();
            ReceiveAsync<Signal>(HandleCurrentJobSignal);
            Receive<ExecuteSql>(executeSql =>
            {
                // Defer request until existing job is complete.
                Stash.Stash();
            });
        }

        /// <summary>
        ///     Called when the actor is waiting for an existing job to finish.
        /// </summary>
        void WaitingForExistingJob()
        {
            StartPolling();

            ReceiveAsync<Signal>(HandlePreviousJobSignal);
            Receive<ExecuteSql>(executeSql =>
            {
                // Defer request until existing job is complete.
                Stash.Stash();
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
        async Task Execute()
        {
            V1Service serverService = await FindServerService();
            if (serverService == null)
            {
                Log.Error("Cannot find Service for server {ServerId}.", Server.Id);

                Owner.Tell(
                    new Status.Failure(new Exception(
                        message: $"Cannot find Service for server {Server.Id}."
                    ))
                );

                Context.Stop(Self);

                return;
            }

            Job = await FindJob();
            if (Job != null)
            {
                Log.Info("Found existing job {JobName}.", Job.Metadata.Name);

                if (Job.Status.Active == null || Job.Status.Active == 0)
                {
                    Log.Info("Deleting existing job {JobName}...", Job.Metadata.Name);

                    await KubeClient.JobsV1.Delete(
                        Job.Metadata.Name
                    );

                    Log.Info("Deleted existing job {JobName}.", Job.Metadata.Name);
                }
                else
                {
                    Log.Info("Existing job {JobName} still has {ActivePodCount} active pods; will wait {JobCompletionTimeout} before forcing job termination...",
                        Job.Metadata.Name,
                        Job.Status.Active,
                        JobCompletionTimeout
                    );

                    // Existing job is running; wait for it to terminate.
                    Become(WaitingForExistingJob);

                    return;
                }
            }

            V1Secret secret = await EnsureSecretPresent();

            V1ConfigMap configMap = await EnsureConfigMapPresent(serverService);

            string jobName = KubeResources.GetJobName(CurrentRequest, Server);

            Log.Info("Creating T-SQL Job {JobName} for database {DatabaseName} in server {ServerId}...",
                jobName,
                CurrentRequest.DatabaseName,
                Server.Id
            );

            try
            {
                Job = await KubeClient.JobsV1.Create(new V1Job
                {
                    ApiVersion = "batch/v1",
                    Kind = "Job",
                    Metadata = new V1ObjectMeta
                    {
                        Name = jobName,
                        Labels = new Dictionary<string, string>
                        {
                            ["cloud.dimensiondata.daas.server-id"] = Server.Id.ToString(),
                            ["cloud.dimensiondata.daas.database"] = CurrentRequest.DatabaseName
                        }
                    },
                    Spec = KubeSpecs.ExecuteSql(Server,
                        secretName: secret.Metadata.Name,
                        configMapName: configMap.Metadata.Name
                    )
                });

                Log.Info("Created T-SQL Job {JobName} for database {DatabaseName} in server {ServerId}...",
                    jobName,
                    CurrentRequest.DatabaseName,
                    Server.Id
                );

                Become(ExecutingJob);
            }
            catch (HttpRequestException<UnversionedStatus> createFailed)
            {
                Log.Error("Failed to create Job {JobName} for database {DatabaseName} in server {ServerId} (Message:{FailureMessage}, Reason:{FailureReason}).",
                    jobName,
                    CurrentRequest.DatabaseName,
                    Server.Id,
                    createFailed.Response.Message,
                    createFailed.Response.Reason
                );

                Owner.Tell(
                    new Status.Failure(createFailed)
                );

                Context.Stop(Self);
            }
        }

        /// <summary>
        ///     Handle a signal while waiting for a newly-created job to complete.
        /// </summary>
        /// <param name="signal">
        ///     The signal to handle.
        /// </param>
        /// <returns>
        ///     A <see cref="Task"/> representing the operation.
        /// </returns>
        async Task HandleCurrentJobSignal(Signal signal)
        {
            switch (signal)
            {
                case Signal.PollJobStatus:
                {
                    Log.Info("Checking status of current T-SQL Job {JobName}...",
                        Job.Metadata.Name
                    );

                    string jobName = Job.Metadata.Name;
                    
                    Job = await KubeClient.JobsV1.Get(jobName);
                    if (Job == null)
                    {
                        Log.Info("Job {JobName} not found; will treat as failed.", jobName);

                        Owner.Tell(
                            new SqlExecuted(jobName, Server.Id, CurrentRequest.DatabaseName, SqlExecutionResult.JobDeleted,
                                output: "T-SQL job was deleted."
                            )
                        );

                        Become(Ready);
                    }

                    if (Job.Status.Active == 0 || Job.Status.Active == null)
                    {
                        // TODO: This is a dodgy way to process the job's conditions. There can be multiple conditions.
                        V1JobCondition jobCondition = Job.Status.Conditions[0];
                        if (jobCondition.Type == "Complete")
                        {
                            Log.Info("Job {JobName} has successfully completed.",
                                Job.Metadata.Name
                            );

                            Owner.Tell(
                                new SqlExecuted(jobName, Server.Id, CurrentRequest.DatabaseName, SqlExecutionResult.Success,
                                    output: "T-SQL executed successfully." // TODO: Collect and use Pod logs here.
                                )
                            );
                        }
                        else
                        {
                            Log.Info("Job {JobName} failed ({FailureReason}: {FailureMessage}).",
                                Job.Metadata.Name,
                                jobCondition.Reason,
                                jobCondition.Message                                    
                            );

                            Owner.Tell(
                                new SqlExecuted(jobName, Server.Id, CurrentRequest.DatabaseName, SqlExecutionResult.Failed,
                                    output: $"Job {jobName} failed ({jobCondition.Reason}: {jobCondition.Message})."
                                )
                            );
                        }

                        Become(Ready);
                    }
                    else
                    {
                        Log.Info("Job {JobName} is still running ({ActivePodCount} active pods).",
                            Job.Metadata.Name,
                            Job.Status.Active
                        );
                    }

                    break;
                }
                case Signal.Timeout:
                {
                    string jobName = Job.Metadata.Name;

                    Log.Info("Timed out after waiting {JobCompletionTimeout} for current T-SQL Job {JobName} to complete.", JobCompletionTimeout, jobName);
                    
                    Job = await KubeClient.JobsV1.Get(jobName);
                    if (Job != null)
                    {
                        Log.Info("Deleting Job {JobName}...", jobName);

                        await KubeClient.JobsV1.Delete(jobName);
                        Job = null;

                        Log.Info("Deleted Job {JobName}.", jobName);
                    }
                    else
                        Log.Info("Job {JobName} not found; will treat as completed.", jobName);

                    Owner.Tell(
                        new SqlExecuted(jobName, Server.Id, CurrentRequest.DatabaseName, SqlExecutionResult.JobTimeout,
                            output: "Timed out waiting for an existing T-SQL job to complete." // TODO: Collect this from pod logs.
                        )
                    );

                    Become(Ready);

                    break;
                }
                default:
                {
                    Unhandled(signal);

                    break;
                }
            }
        }

        /// <summary>
        ///     Handle a signal while waiting for a previous job to complete.
        /// </summary>
        /// <param name="signal">
        ///     The signal to handle.
        /// </param>
        /// <returns>
        ///     A <see cref="Task"/> representing the operation.
        /// </returns>
        async Task HandlePreviousJobSignal(Signal signal)
        {
            switch (signal)
            {
                case Signal.PollJobStatus:
                {
                    Log.Info("Checking status of previous T-SQL Job {JobName}...",
                        Job.Metadata.Name
                    );

                    string jobName = Job.Metadata.Name;
                    
                    Job = await KubeClient.JobsV1.Get(jobName);
                    if (Job == null)
                    {
                        Log.Info("Job {JobName} not found; will treat as completed.", jobName);

                        Become(ExecutingJob);
                    }

                    if (Job.Status.Active == null || Job.Status.Active == 0)
                    {
                        if (Job.Status.Conditions[0].Type == "Complete")
                        {
                            Log.Info("Job {JobName} has successfully completed.",
                                Job.Metadata.Name
                            );
                        }
                        else
                        {
                            Log.Info("Job {JobName} failed ({FailureReason}:{FailureMessage}).",
                                Job.Metadata.Name,
                                Job.Status.Conditions[0].Reason,
                                Job.Status.Conditions[0].Message                                    
                            );
                        }

                        Become(ExecutingJob);
                    }
                    else
                    {
                        Log.Info("Job {JobName} is still running ({ActivePodCount} active pods).",
                            Job.Metadata.Name,
                            Job.Status.Active
                        );
                    }

                    break;
                }
                case Signal.Timeout:
                {
                    string jobName = Job.Metadata.Name;
                    string databaseName;
                    Job.Metadata.Labels.TryGetValue("cloud.dimensiondata.daas.database", out databaseName);

                    Log.Info("Timed out after waiting {JobCompletionTimeout} for previous Job {JobName} to complete.", JobCompletionTimeout, jobName);
                    
                    Job = await KubeClient.JobsV1.Get(jobName);
                    if (Job != null)
                    {
                        Log.Info("Deleting Job {JobName}...", jobName);

                        await KubeClient.JobsV1.Delete(jobName);
                        Job = null;

                        Log.Info("Deleted Job {JobName}.", jobName);
                    }
                    else
                        Log.Info("Job {JobName} not found; will treat as completed.", jobName);

                    Owner.Tell(
                        new SqlExecuted(jobName, Server.Id, CurrentRequest.DatabaseName, SqlExecutionResult.JobTimeout,
                            output: "Timed out waiting for an existing T-SQL job to complete." // TODO: Collect this from pod logs.
                        )
                    );

                    Become(Ready);

                    break;
                }
                default:
                {
                    Unhandled(signal);

                    break;
                }
            }
        }

        /// <summary>
        ///     Start the polling and timeout signals.
        /// </summary>
        void StartPolling()
        {
            Log.Debug("Starting the polling and timeout signals...");

            if (_pollCancellation != null || _timeoutCancellation != null)
            {
                Log.Warning("The polling and / or timeout signals are already active; cancelling...");
                
                StopPolling();
            }

            _pollCancellation = Context.System.Scheduler.ScheduleTellRepeatedlyCancelable(
                initialDelay: TimeSpan.FromSeconds(5),
                interval: TimeSpan.FromSeconds(5),
                receiver: Self,
                message: Signal.PollJobStatus,
                sender: Self
            );

            _timeoutCancellation = Context.System.Scheduler.ScheduleTellOnceCancelable(
                delay: JobCompletionTimeout,
                receiver: Self,
                message: Signal.Timeout,
                sender: Self
            );

            Log.Debug("The polling and timeout signals have been started.");
        }

        /// <summary>
        ///     Cancel the polling and timeout signals.
        /// </summary>
        void StopPolling()
        {
            if (_timeoutCancellation == null && _pollCancellation == null)
                return; // Nothing to do.

            Log.Debug("Stopping the polling and / or timeout signals...");

            _timeoutCancellation?.Cancel();
            _timeoutCancellation = null;
            
            _pollCancellation?.Cancel();
            _pollCancellation = null;

            Log.Debug("The polling and / or timeout have been stopped.");
        }

        /// <summary>
        ///     Find the current Job (if any) used to execute the specified T-SQL.
        /// </summary>
        /// <returns>
        ///     The Job, or <c>null</c> if it was not found.
        /// </returns>
        async Task<V1Job> FindJob()
        {
            string jobName = KubeResources.GetJobName(CurrentRequest, Server);

            return await KubeClient.JobsV1.Get(jobName);
        }

        /// <summary>
        ///     Find the pod for the current Job (if any) used to execute the specified T-SQL.
        /// </summary>
        /// <returns>
        ///     The Pod, or <c>null</c> if it was not found.
        /// </returns>
        async Task<V1Pod> FindJobPod()
        {
            string jobName = KubeResources.GetJobName(CurrentRequest, Server);

            List<V1Pod> matchingPods = await KubeClient.PodsV1.List(
                labelSelector: $"job-name = {jobName}"
            );
            if (matchingPods.Count == 0)
                return null;

            return matchingPods[matchingPods.Count - 1];
        }

        /// <summary>
        ///     Find the secret used to execute T-SQL in the specified database.
        /// </summary>
        /// <returns>
        ///     A <see cref="V1Secret"/> representing the secret, or <c>null</c>, if the secret was not found.
        /// </returns>
        async Task<V1Secret> FindSecret()
        {
            return await KubeClient.SecretsV1.Get(
                KubeResources.GetJobName(CurrentRequest, Server)
            );
        }

        /// <summary>
        ///     Ensure that the Kubernetes Secret exists for executing the specified T-SQL (creating it if necessary).
        /// </summary>
        /// <returns>
        ///     <c>true</c>, if the secret exists; otherwise, <c>false</c>.
        /// </returns>
        async Task<V1Secret> EnsureSecretPresent()
        {
            V1Secret existingSecret = await FindSecret();
            if (existingSecret != null)
            {
                Log.Info("Found existing secret {SecretName} for database {DatabaseName} in server {ServerId}.",
                    existingSecret.Metadata.Name,
                    CurrentRequest.DatabaseName,
                    Server.Id
                );

                return existingSecret;
            }

            var newSecret = new V1Secret
            {
                ApiVersion = "v1",
                Kind = "Secret",
                Metadata = new V1ObjectMeta
                {
                    Name = KubeResources.GetJobName(CurrentRequest, Server),
                    Labels = new Dictionary<string, string>
                    {
                        ["cloud.dimensiondata.daas.server-id"] = Server.Id.ToString(),
                        ["cloud.dimensiondata.daas.database"] = CurrentRequest.DatabaseName,
                        ["cloud.dimensiondata.daas.action"] = "exec-sql"
                    }
                },
                Type = "Opaque",
                Data = new Dictionary<string, string>()
            };
            newSecret.AddData("database-user", "sa");
            newSecret.AddData("database-password", Server.AdminPassword);
            newSecret.AddData("secrets.sql", $@"
                :setvar DatabaseName '{CurrentRequest.DatabaseName}'
                :setvar DatabaseUser 'sa'
                :setvar DatabasePassword '{Server.AdminPassword}'
            ");

            V1Secret createdSecret;
            try
            {
                createdSecret = await KubeClient.SecretsV1.Create(newSecret);
            }
            catch (HttpRequestException<UnversionedStatus> createFailed)
            {
                Log.Error("Failed to create Secret {SecretName} for database {DatabaseName} in server {ServerId} (Message:{FailureMessage}, Reason:{FailureReason}).",
                    newSecret.Metadata.Name,
                    CurrentRequest.DatabaseName,
                    Server.Id,
                    createFailed.Response.Message,
                    createFailed.Response.Reason
                );

                throw;
            }

            Log.Info("Successfully created secret {SecretName} for database {DatabaseName} in server {ServerId}.",
                createdSecret.Metadata.Name,
                CurrentRequest.DatabaseName,
                Server.Id
            );

            return createdSecret;
        }

        /// <summary>
        ///     Ensure that the Kubernetes Secret does not exist for executing T-SQL in the specified database (deleting it if necessary).
        /// </summary>
        /// <returns>
        ///     <c>true</c>, if the secret does not exist; otherwise, <c>false</c>.
        /// </returns>
        async Task<bool> EnsureSecretAbsent()
        {
            V1Secret secret = await FindSecret();
            if (secret == null)
                return true;

            Log.Info("Deleting secret {SecretName} for database {DatabaseName} in server {ServerId}...",
                secret.Metadata.Name,
                CurrentRequest.DatabaseName,
                Server.Id
            );

            try
            {
                await KubeClient.SecretsV1.Delete(
                    name: secret.Metadata.Name
                );
            }
            catch (HttpRequestException<UnversionedStatus> deleteFailed)
            {
                Log.Error("Failed to delete secret {SecretName} for database {DatabaseName} in server {ServerId} (Message:{FailureMessage}, Reason:{FailureReason}).",
                    secret.Metadata.Name,
                    CurrentRequest.DatabaseName,
                    Server.Id,
                    deleteFailed.Response.Message,
                    deleteFailed.Response.Reason
                );

                return false;
            }

            Log.Info("Deleted secret {SecretName} for database {DatabaseName} in server {ServerId}.",
                secret.Metadata.Name,
                CurrentRequest.DatabaseName,
                Server.Id
            );

            return true;
        }

        /// <summary>
        ///     Find the ConfigMap used to execute T-SQL in the specified database.
        /// </summary>
        /// <returns>
        ///     A <see cref="V1ConfigMap"/> representing the ConfigMap, or <c>null</c>, if the ConfigMap was not found.
        /// </returns>
        async Task<V1ConfigMap> FindConfigMap()
        {
            return await KubeClient.ConfigMapsV1.Get(
                name: KubeResources.GetJobName(CurrentRequest, Server)
            );
        }

        /// <summary>
        ///     Ensure that the Kubernetes ConfigMap exists for executing T-SQL in the specified database (creating it if necessary).
        /// </summary>
        /// <param name="serverService">
        ///     The Service used to communicate with the target instance of SQL Server.
        /// </param>
        /// <returns>
        ///     <c>true</c>, if the ConfigMap exists; otherwise, <c>false</c>.
        /// </returns>
        async Task<V1ConfigMap> EnsureConfigMapPresent(V1Service serverService)
        {
            if (serverService == null)
                throw new ArgumentNullException(nameof(serverService));

            V1ConfigMap existingConfigMap = await FindConfigMap();
            if (existingConfigMap != null)
            {
                Log.Info("Found existing ConfigMap {ConfigMapName} for database {DatabaseName} in server {ServerId}.",
                    existingConfigMap.Metadata.Name,
                    CurrentRequest.DatabaseName,
                    Server.Id
                );

                return existingConfigMap;
            }

            var newConfigMap = new V1ConfigMap
            {
                ApiVersion = "v1",
                Kind = "ConfigMap",
                Metadata = new V1ObjectMeta
                {
                    Name = KubeResources.GetJobName(CurrentRequest, Server),
                    Labels = new Dictionary<string, string>
                    {
                        ["cloud.dimensiondata.daas.server-id"] = Server.Id.ToString(),
                        ["cloud.dimensiondata.daas.database"] = CurrentRequest.DatabaseName,
                        ["cloud.dimensiondata.daas.action"] = "exec-sql"
                    }
                },
                Data = new Dictionary<string, string>()
            };
            newConfigMap.AddData("database-server",
                value: $"{serverService.Metadata.Name}.{serverService.Metadata.Namespace}.svc.cluster.local,{serverService.Spec.Ports[0].Port}"
            );
            newConfigMap.AddData("database-name",
                value: CurrentRequest.DatabaseName
            );
            newConfigMap.AddData("script.sql",
                value: CurrentRequest.Sql
            );

            V1ConfigMap createdConfigMap;
            try
            {
                createdConfigMap = await KubeClient.ConfigMapsV1.Create(newConfigMap);
            }
            catch (HttpRequestException<UnversionedStatus> createFailed)
            {
                Log.Error("Failed to create ConfigMap {ConfigMapName} for database {DatabaseName} in server {ServerId} (Message:{FailureMessage}, Reason:{FailureReason}).",
                    newConfigMap.Metadata.Name,
                    CurrentRequest.DatabaseName,
                    Server.Id,
                    createFailed.Response.Message,
                    createFailed.Response.Reason
                );

                throw;
            }

            Log.Info("Successfully created ConfigMap {ConfigMapName} for database {DatabaseName} in server {ServerId}.",
                createdConfigMap.Metadata.Name,
                CurrentRequest.DatabaseName,
                Server.Id
            );

            return createdConfigMap;
        }

        /// <summary>
        ///     Ensure that the Kubernetes ConfigMap does not exist for executing T-SQL in the specified database (deleting it if necessary).
        /// </summary>
        /// <returns>
        ///     <c>true</c>, if the ConfigMap does not exist; otherwise, <c>false</c>.
        /// </returns>
        async Task<bool> EnsureConfigMapAbsent()
        {
            V1ConfigMap configMap = await FindConfigMap();
            if (configMap == null)
                return true;

            Log.Info("Deleting ConfigMap {ConfigMapName} for database {DatabaseName} in server {ServerId}...",
                configMap.Metadata.Name,
                CurrentRequest.DatabaseName,
                Server.Id
            );

            try
            {
                await KubeClient.ConfigMapsV1.Delete(
                    name: configMap.Metadata.Name
                );
            }
            catch (HttpRequestException<UnversionedStatus> deleteFailed)
            {
                Log.Error("Failed to delete ConfigMap {ConfigMapName} for database {DatabaseName} in server {ServerId} (Message:{FailureMessage}, Reason:{FailureReason}).",
                    configMap.Metadata.Name,
                    CurrentRequest.DatabaseName,
                    Server.Id,
                    deleteFailed.Response.Message,
                    deleteFailed.Response.Reason
                );

                return false;
            }

            Log.Info("Deleted ConfigMap {ConfigMapName} for database {DatabaseName} in server {ServerId}.",
                configMap.Metadata.Name,
                CurrentRequest.DatabaseName,
                Server.Id
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
            List<V1Service> matchingServices = await KubeClient.ServicesV1.List(
                kubeNamespace: "default",
                labelSelector: $"cloud.dimensiondata.daas.server-id = {Server.Id}"
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
        ///     Well-known signals understood by the <see cref="SqlRunner"/> actor.
        /// </summary>
        enum Signal
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
