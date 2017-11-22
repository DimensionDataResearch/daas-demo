using HTTPlease;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DaaSDemo.Provisioning.Provisioners
{
    using Common.Options;
    using Exceptions;
    using KubeClient;
    using KubeClient.Models;
    using Models.Data;
    using Models.Sql;
    using SqlExecutor.Client;

    /// <summary>
    ///     Provisioning facility for a <see cref="DatabaseServer"/>.
    /// </summary>
    public class DatabaseServerProvisioner
    {
        /// <summary>
        ///     Create a new <see cref="DatabaseServerProvisioner"/>.
        /// </summary>
        /// <param name="logger">
        ///     The provisioner's logger.
        /// </param>
        /// <param name="kubeClient">
        ///     The <see cref="KubeApiClient"/> used to communicate with the Kubernetes API.
        /// </param>
        /// <param name="kubeOptions">
        ///     Application-level Kubernetes settings.
        /// </param>
        /// <param name="kubeResources">
        ///     A factory for Kubernetes resource models.
        /// </param>
        /// <param name="sqlClient">
        ///     The <see cref="SqlApiClient"/> used to communicate with the SQL Executor API.
        /// </param>
        public DatabaseServerProvisioner(ILogger<DatabaseServerProvisioner> logger, KubeApiClient kubeClient, SqlApiClient sqlClient, IOptions<KubernetesOptions> kubeOptions, KubeResources kubeResources)
        {
            if (logger == null)
                throw new ArgumentNullException(nameof(logger));

            if (kubeClient == null)
                throw new ArgumentNullException(nameof(kubeClient));

            if (sqlClient == null)
                throw new ArgumentNullException(nameof(sqlClient));
            
            if (kubeOptions == null)
                throw new ArgumentNullException(nameof(kubeOptions));
            
            if (kubeResources == null)
                throw new ArgumentNullException(nameof(kubeResources));

            Log = logger;
            KubeClient = kubeClient;
            SqlClient = sqlClient;
            KubeOptions = kubeOptions.Value;
            KubeResources = kubeResources;
        }

        /// <summary>
        ///     The provisioner's logger.
        /// </summary>
        public ILogger Log { get; set; }

        /// <summary>
        ///     The server's current state (if known).
        /// </summary>
        public DatabaseServer State { get; set; }

        /// <summary>
        ///     The <see cref="KubeApiClient"/> used to communicate with the Kubernetes API.
        /// </summary>
        KubeApiClient KubeClient { get; }

        /// <summary>
        ///     The <see cref="SqlApiClient"/> used to communicate with the SQL Executor API.
        /// </summary>
        SqlApiClient SqlClient { get; }

        /// <summary>
        ///     Application-level Kubernetes settings.
        /// </summary>
        KubernetesOptions KubeOptions { get; }

        /// <summary>
        ///     A factory for Kubernetes resource models.
        /// </summary>
        KubeResources KubeResources { get; }

        /// <summary>
        ///     Find the server's associated PersistentVolumeClaim for data (if it exists).
        /// </summary>
        /// <returns>
        ///     The PersistentVolumeClaim, or <c>null</c> if it was not found.
        /// </returns>
        public async Task<PersistentVolumeClaimV1> FindDataVolumeClaim()
        {
            RequireCurrentState();

            List<PersistentVolumeClaimV1> matchingPersistentVolumeClaims = await KubeClient.PersistentVolumeClaimsV1().List(
                labelSelector: $"cloud.dimensiondata.daas.server-id = {State.Id}, cloud.dimensiondata.daas.volume-type = data",
                kubeNamespace: KubeOptions.KubeNamespace
            );

            if (matchingPersistentVolumeClaims.Count == 0)
                return null;

            return matchingPersistentVolumeClaims[matchingPersistentVolumeClaims.Count - 1];
        }

        /// <summary>
        ///     Find the server's associated Deployment (if it exists).
        /// </summary>
        /// <returns>
        ///     The Deployment, or <c>null</c> if it was not found.
        /// </returns>
        public async Task<DeploymentV1Beta1> FindDeployment()
        {
            RequireCurrentState();

            List<DeploymentV1Beta1> matchingDeployments = await KubeClient.DeploymentsV1Beta1().List(
                labelSelector: $"cloud.dimensiondata.daas.server-id = {State.Id}",
                kubeNamespace: KubeOptions.KubeNamespace
            );

            if (matchingDeployments.Count == 0)
                return null;

            return matchingDeployments[matchingDeployments.Count - 1];
        }

        /// <summary>
        ///     Find the server's associated internally-facing Service (if it exists).
        /// </summary>
        /// <returns>
        ///     The Service, or <c>null</c> if it was not found.
        /// </returns>
        public async Task<ServiceV1> FindInternalService()
        {
            RequireCurrentState();

            List<ServiceV1> matchingServices = await KubeClient.ServicesV1().List(
                labelSelector: $"cloud.dimensiondata.daas.server-id = {State.Id},cloud.dimensiondata.daas.service-type = internal",
                kubeNamespace: KubeOptions.KubeNamespace
            );
            if (matchingServices.Count == 0)
                return null;

            return matchingServices[matchingServices.Count - 1];
        }

        /// <summary>
        ///     Find the server's associated ServiceMonitor (if it exists).
        /// </summary>
        /// <returns>
        ///     The ServiceMonitor, or <c>null</c> if it was not found.
        /// </returns>
        public async Task<PrometheusServiceMonitorV1> FindServiceMonitor()
        {
            RequireCurrentState();

            List<PrometheusServiceMonitorV1> matchingServices = await KubeClient.PrometheusServiceMonitorsV1().List(
                labelSelector: $"cloud.dimensiondata.daas.server-id = {State.Id},cloud.dimensiondata.daas.monitor-type = sql-server",
                kubeNamespace: KubeOptions.KubeNamespace
            );
            if (matchingServices.Count == 0)
                return null;

            return matchingServices[matchingServices.Count - 1];
        }

        /// <summary>
        ///     Find the server's associated externally-facing Service (if it exists).
        /// </summary>
        /// <returns>
        ///     The Service, or <c>null</c> if it was not found.
        /// </returns>
        public async Task<ServiceV1> FindExternalService()
        {
            RequireCurrentState();

            List<ServiceV1> matchingServices = await KubeClient.ServicesV1().List(
                labelSelector: $"cloud.dimensiondata.daas.server-id = {State.Id},cloud.dimensiondata.daas.service-type = external",
                kubeNamespace: KubeOptions.KubeNamespace
            );
            if (matchingServices.Count == 0)
                return null;

            return matchingServices[matchingServices.Count - 1];
        }

        /// <summary>
        ///     Ensure that a PersistentVolumeClaim for data exists for the specified database server.
        /// </summary>
        /// <returns>
        ///     The PersistentVolumeClaim resource, as a <see cref="PersistentVolumeClaimV1"/>.
        /// </returns>
        public async Task<PersistentVolumeClaimV1> EnsureDataVolumeClaimPresent()
        {
            RequireCurrentState();

            PersistentVolumeClaimV1 existingPersistentVolumeClaim = await FindDataVolumeClaim();
            if (existingPersistentVolumeClaim != null)
            {
                Log.LogInformation("Found existing data-volume claim {PersistentVolumeClaimName} for server {ServerId}.",
                    existingPersistentVolumeClaim.Metadata.Name,
                    State.Id
                );

                return existingPersistentVolumeClaim;
            }

            Log.LogInformation("Creating data-volume claim for server {ServerId}...",
                State.Id
            );

            PersistentVolumeClaimV1 createdPersistentVolumeClaim = await KubeClient.PersistentVolumeClaimsV1().Create(
                KubeResources.DataVolumeClaim(State,
                    kubeNamespace: KubeOptions.KubeNamespace
                )
            );

            Log.LogInformation("Successfully created data-volume claim {PersistentVolumeClaimName} for server {ServerId}.",
                createdPersistentVolumeClaim.Metadata.Name,
                State.Id
            );

            return createdPersistentVolumeClaim;
        }

        /// <summary>
        ///     Ensure that a PersistentVolumeClaim for data does not exist for the specified database server.
        /// </summary>
        /// <returns>
        ///     <c>true</c>, if the controller is now absent; otherwise, <c>false</c>.
        /// </returns>
        public async Task<bool> EnsureDataVolumeClaimAbsent()
        {
            RequireCurrentState();

            PersistentVolumeClaimV1 dataVolumeClaim = await FindDataVolumeClaim();
            if (dataVolumeClaim == null)
                return true;

            Log.LogInformation("Deleting data-volume claim {PersistentVolumeClaimName} for server {ServerId}...",
                dataVolumeClaim.Metadata.Name,
                State.Id
            );

            try
            {
                await KubeClient.PersistentVolumeClaimsV1().Delete(
                    name: dataVolumeClaim.Metadata.Name,
                    kubeNamespace: KubeOptions.KubeNamespace,
                    propagationPolicy: DeletePropagationPolicy.Background
                );

                string dataVolumeName = dataVolumeClaim.Spec.VolumeName;
                if (!String.IsNullOrWhiteSpace(dataVolumeClaim.Spec.VolumeName))
                {
                    Log.LogInformation("Deleting data volume {PersistentVolumeName} for server {ServerId}...",
                        dataVolumeName,
                        State.Id
                    );

                    await KubeClient.PersistentVolumesV1().Delete(
                        name: dataVolumeName,
                        kubeNamespace: KubeOptions.KubeNamespace
                    );
                }
            }
            catch (HttpRequestException<StatusV1> deleteFailed)
            {
                Log.LogError("Failed to delete data-volume claim {PersistentVolumeClaimName} for server {ServerId} (Message:{FailureMessage}, Reason:{FailureReason}).",
                    dataVolumeClaim.Metadata.Name,
                    State.Id,
                    deleteFailed.Response.Message,
                    deleteFailed.Response.Reason
                );

                return false;
            }

            Log.LogInformation("Deleted data-volume claim {PersistentVolumeClaimName} for server {ServerId}.",
                dataVolumeClaim.Metadata.Name,
                State.Id
            );

            return true;
        }

        /// <summary>
        ///     Ensure that a Deployment resource exists for the specified database server.
        /// </summary>
        /// <returns>
        ///     The Deployment resource, as a <see cref="DeploymentV1Beta1"/>.
        /// </returns>
        public async Task<DeploymentV1Beta1> EnsureDeploymentPresent()
        {
            RequireCurrentState();

            DeploymentV1Beta1 existingDeployment = await FindDeployment();
            if (existingDeployment != null)
            {
                Log.LogInformation("Found existing deployment {DeploymentName} for server {ServerId}.",
                    existingDeployment.Metadata.Name,
                    State.Id
                );

                return existingDeployment;
            }

            Log.LogInformation("Creating deployment for server {ServerId}...",
                State.Id
            );

            DeploymentV1Beta1 createdDeployment = await KubeClient.DeploymentsV1Beta1().Create(
                KubeResources.Deployment(State,
                    kubeNamespace: KubeOptions.KubeNamespace
                )
            );

            Log.LogInformation("Successfully created deployment {DeploymentName} for server {ServerId}.",
                createdDeployment.Metadata.Name,
                State.Id
            );

            return createdDeployment;
        }

        /// <summary>
        ///     Ensure that a Deployment resource does not exist for the specified database server.
        /// </summary>
        /// <returns>
        ///     <c>true</c>, if the controller is now absent; otherwise, <c>false</c>.
        /// </returns>
        public async Task<bool> EnsureDeploymentAbsent()
        {
            RequireCurrentState();

            DeploymentV1Beta1 controller = await FindDeployment();
            if (controller == null)
                return true;

            Log.LogInformation("Deleting deployment {DeploymentName} for server {ServerId}...",
                controller.Metadata.Name,
                State.Id
            );

            try
            {
                await KubeClient.DeploymentsV1Beta1().Delete(
                    name: controller.Metadata.Name,
                    kubeNamespace: KubeOptions.KubeNamespace,
                    propagationPolicy: DeletePropagationPolicy.Background
                );
            }
            catch (HttpRequestException<StatusV1> deleteFailed)
            {
                Log.LogError("Failed to delete deployment {DeploymentName} for server {ServerId} (Message:{FailureMessage}, Reason:{FailureReason}).",
                    controller.Metadata.Name,
                    State.Id,
                    deleteFailed.Response.Message,
                    deleteFailed.Response.Reason
                );

                return false;
            }

            Log.LogInformation("Deleted deployment {DeploymentName} for server {ServerId}.",
                controller.Metadata.Name,
                State.Id
            );

            return true;
        }

        /// <summary>
        ///     Ensure that an internally-facing Service resource exists for the specified database server.
        /// </summary>
        /// <returns>
        ///     The Service resource, as a <see cref="ServiceV1"/>.
        /// </returns>
        public async Task EnsureInternalServicePresent()
        {
            RequireCurrentState();

            ServiceV1 existingInternalService = await FindInternalService();
            if (existingInternalService == null)
            {
                Log.LogInformation("Creating internal service for server {ServerId}...",
                    State.Id
                );

                ServiceV1 createdService = await KubeClient.ServicesV1().Create(
                    KubeResources.InternalService(State,
                        kubeNamespace: KubeOptions.KubeNamespace
                    )
                );

                Log.LogInformation("Successfully created internal service {ServiceName} for server {ServerId}.",
                    createdService.Metadata.Name,
                    State.Id
                );
            }
            else
            {
                Log.LogInformation("Found existing internal service {ServiceName} for server {ServerId}.",
                    existingInternalService.Metadata.Name,
                    State.Id
                );
            }
        }

        /// <summary>
        ///     Ensure that an internally-facing Service resource does not exist for the specified database server.
        /// </summary>
        public async Task EnsureInternalServiceAbsent()
        {
            RequireCurrentState();

            ServiceV1 existingInternalService = await FindInternalService();
            if (existingInternalService != null)
            {
                Log.LogInformation("Deleting internal service {ServiceName} for server {ServerId}...",
                    existingInternalService.Metadata.Name,
                    State.Id
                );

                StatusV1 result = await KubeClient.ServicesV1().Delete(
                    name: existingInternalService.Metadata.Name,
                    kubeNamespace: KubeOptions.KubeNamespace
                );

                if (result.Status != "Success" && result.Reason != "NotFound")
                {
                    Log.LogError("Failed to delete internal service {ServiceName} for server {ServerId} (Message:{FailureMessage}, Reason:{FailureReason}).",
                        existingInternalService.Metadata.Name,
                        State.Id,
                        result.Message,
                        result.Reason
                    );
                }

                Log.LogInformation("Deleted internal service {ServiceName} for server {ServerId}.",
                    existingInternalService.Metadata.Name,
                    State.Id
                );
            }
        }

        /// <summary>
        ///     Ensure that a ServiceMonitor resource exists for the specified database server.
        /// </summary>
        /// <returns>
        ///     The Service resource, as a <see cref="ServiceV1"/>.
        /// </returns>
        public async Task EnsureServiceMonitorPresent()
        {
            RequireCurrentState();

            PrometheusServiceMonitorV1 existingServiceMonitor = await FindServiceMonitor();
            if (existingServiceMonitor == null)
            {
                Log.LogInformation("Creating service monitor for server {ServerId}...",
                    State.Id
                );

                PrometheusServiceMonitorV1 createdService = await KubeClient.PrometheusServiceMonitorsV1().Create(
                    KubeResources.ServiceMonitor(State,
                        kubeNamespace: KubeOptions.KubeNamespace
                    )
                );

                Log.LogInformation("Successfully created service monitor {ServiceName} for server {ServerId}.",
                    createdService.Metadata.Name,
                    State.Id
                );
            }
            else
            {
                Log.LogInformation("Found existing service monitor {ServiceName} for server {ServerId}.",
                    existingServiceMonitor.Metadata.Name,
                    State.Id
                );
            }
        }

        /// <summary>
        ///     Ensure that a ServiceMonitor resource does not exist for the specified database server.
        /// </summary>
        public async Task EnsureServiceMonitorAbsent()
        {
            RequireCurrentState();

            PrometheusServiceMonitorV1 existingServiceMonitor = await FindServiceMonitor();
            if (existingServiceMonitor != null)
            {
                Log.LogInformation("Deleting service monitor {ServiceName} for server {ServerId}...",
                    existingServiceMonitor.Metadata.Name,
                    State.Id
                );

                StatusV1 result = await KubeClient.PrometheusServiceMonitorsV1().Delete(
                    name: existingServiceMonitor.Metadata.Name,
                    kubeNamespace: KubeOptions.KubeNamespace
                );

                if (result.Status != "Success" && result.Reason != "NotFound")
                {
                    Log.LogError("Failed to delete service monitor {ServiceName} for server {ServerId} (Message:{FailureMessage}, Reason:{FailureReason}).",
                        existingServiceMonitor.Metadata.Name,
                        State.Id,
                        result.Message,
                        result.Reason
                    );
                }

                Log.LogInformation("Deleted service monitor {ServiceName} for server {ServerId}.",
                    existingServiceMonitor.Metadata.Name,
                    State.Id
                );
            }
        }

        /// <summary>
        ///     Ensure that an externally-facing Service resource exists for the specified database server.
        /// </summary>
        /// <returns>
        ///     A <see cref="Task"/> representing the operation.
        /// </returns>
        public async Task EnsureExternalServicePresent()
        {
            RequireCurrentState();

            ServiceV1 existingExternalService = await FindExternalService();
            if (existingExternalService == null)
            {
                Log.LogInformation("Creating external service for server {ServerId}...",
                    State.Id
                );

                ServiceV1 createdService = await KubeClient.ServicesV1().Create(
                    KubeResources.ExternalService(State,
                        kubeNamespace: KubeOptions.KubeNamespace
                    )
                );

                Log.LogInformation("Successfully created external service {ServiceName} for server {ServerId}.",
                    createdService.Metadata.Name,
                    State.Id
                );
            }
            else
            {
                Log.LogInformation("Found existing external service {ServiceName} for server {ServerId}.",
                    existingExternalService.Metadata.Name,
                    State.Id
                );
            }
        }

        /// <summary>
        ///     Ensure that an externally-facing Service resource does not exist for the specified database server.
        /// </summary>
        public async Task EnsureExternalServiceAbsent()
        {
            RequireCurrentState();

            ServiceV1 existingExternalService = await FindExternalService();
            if (existingExternalService != null)
            {
                Log.LogInformation("Deleting external service {ServiceName} for server {ServerId}...",
                    existingExternalService.Metadata.Name,
                    State.Id
                );

                StatusV1 result = await KubeClient.ServicesV1().Delete(
                    name: existingExternalService.Metadata.Name,
                    kubeNamespace: KubeOptions.KubeNamespace
                );

                if (result.Status != "Success" && result.Reason != "NotFound")
                {
                    Log.LogError("Failed to delete external service {ServiceName} for server {ServerId} (Message:{FailureMessage}, Reason:{FailureReason}).",
                        existingExternalService.Metadata.Name,
                        State.Id,
                        result.Message,
                        result.Reason
                    );
                }

                Log.LogInformation("Deleted external service {ServiceName} for server {ServerId}.",
                    existingExternalService.Metadata.Name,
                    State.Id
                );
            }
        }

        /// <summary>
        ///     Execute T-SQL to initialise the server configuration.
        /// </summary>
        /// <returns>
        ///     A <see cref="Task"/> representing the operation.
        /// </returns>
        public async Task InitialiseServerConfiguration()
        {
            RequireCurrentState();

            Log.LogInformation("Initialising configuration for server {ServerId}...", State.Id);
            
            CommandResult commandResult = await SqlClient.ExecuteCommand(
                serverId: State.Id,
                databaseId: SqlApiClient.MasterDatabaseId,
                sql: ManagementSql.ConfigureServerMemory(maxMemoryMB: 500 * 1024),
                executeAsAdminUser: true
            );

            for (int messageIndex = 0; messageIndex < commandResult.Messages.Count; messageIndex++)
            {
                Log.LogInformation("T-SQL message [{MessageIndex}] from server {ServerId}: {TSqlMessage}",
                    messageIndex,
                    State.Id,
                    commandResult.Messages[messageIndex]
                );
            }

            if (!commandResult.Success)
            {
                foreach (SqlError error in commandResult.Errors)
                {
                    Log.LogWarning("Error encountered while initialising configuration for server {ServerId} ({ErrorKind}: {ErrorMessage})",
                        State.Id,
                        error.Kind,
                        error.Message
                    );
                }

                throw new SqlExecutionException($"One or more errors were encountered while configuring server (Id: {State.Id}).",
                    serverId: State.Id,
                    databaseId: SqlApiClient.MasterDatabaseId,
                    sqlMessages: commandResult.Messages,
                    sqlErrors: commandResult.Errors
                );
            }

            Log.LogInformation("Configuration initialised for server {ServerId}.", State.Id);
        }

        /// <summary>
        ///     Get the public TCP port number on which the database server is accessible.
        /// </summary>
        /// <param name="server">
        ///     A <see cref="DatabaseServer"/> describing the server.
        /// </param>
        /// <param name="kubeNamespace">
        ///     An optional target Kubernetes namespace.
        /// </param>
        /// <returns>
        ///     The port, or <c>null</c> if the externally-facing service for the server cannot be found.
        /// </returns>
        public async Task<int?> GetPublicPort()
        {
            RequireCurrentState();

            List<ServiceV1> matchingServices = await KubeClient.ServicesV1().List(
                labelSelector: $"cloud.dimensiondata.daas.server-id = {State.Id}, cloud.dimensiondata.daas.service-type = external",
                kubeNamespace: KubeOptions.KubeNamespace
            );
            if (matchingServices.Count == 0)
                return null;

            ServiceV1 externalService = matchingServices[matchingServices.Count - 1];

            return externalService.Spec.Ports[0].NodePort;
        }

        /// <summary>
        ///     Ensure that <see cref="State"/> is populated.
        /// </summary>
        void RequireCurrentState()
        {
            if (State == null)
                throw new InvalidOperationException($"Cannot use {nameof(DatabaseServerProvisioner)} without current state.");
        }
    }
}
