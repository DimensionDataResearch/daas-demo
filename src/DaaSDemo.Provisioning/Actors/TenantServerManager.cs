using Akka;
using Akka.Actor;
using HTTPlease;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace DaaSDemo.Provisioning.Actors
{
    using Common.Utilities;
    using Data;
    using KubeClient;
    using KubeClient.Models;
    using Messages;
    using Models.Data;
    using Models.Sql;
    using Exceptions;
    using SqlExecutor.Client;
    using DaaSDemo.Common.Options;
    using Akka.DI.Core;

    /// <summary>
    ///     Actor that represents a tenant's database server and manages its life-cycle.
    /// </summary>
    /// <remarks>
    ///     Management of the server's databases is delegated to a child <see cref="TenantDatabaseManager"/> actor.
    ///
    ///     AF: UGH - this has become a God Class; break it up, please.
    /// </remarks>
    public class TenantServerManager
        : ReceiveActorEx, IWithUnboundedStash
    {
        /// <summary>
        ///     The period between successive polling operations.
        /// </summary>
        public static readonly TimeSpan PollPeriod = TimeSpan.FromSeconds(5);

        /// <summary>
        ///     The length of time before a polling operation times out.
        /// </summary>
        public static readonly TimeSpan PollTimeout = TimeSpan.FromMinutes(5);

        /// <summary>
        ///     References to <see cref="TenantDatabaseManager"/> actors, keyed by database Id.
        /// </summary>
        readonly Dictionary<int, IActorRef> _databaseManagers = new Dictionary<int, IActorRef>();

        /// <summary>
        ///     Cancellation for the current poll timer (if any).
        /// </summary>
        ICancelable _pollCancellation;

        /// <summary>
        ///     Cancellation for the current poll timeout (if any).
        /// </summary>
        ICancelable _timeoutCancellation;

        /// <summary>
        ///     Create a new <see cref="TenantServerManager"/>.
        /// </summary>
        /// <param name="kubeResources">
        ///     The Kubernetes resource factory.
        /// </param>
        /// <param name="kubeClient">
        ///     The <see cref="KubeApiClient"/> used to communicate with the Kubernetes API.
        /// </param>
        /// <param name="kubeOptions">
        ///     Application-level Kubernetes options.
        /// </param>
        /// <param name="sqlClient">
        ///     The <see cref="SqlApiClient"/> used to communicate with the SQL Executor API.
        /// </param>
        public TenantServerManager(KubeResources kubeResources, KubeApiClient kubeClient, IOptions<KubernetesOptions> kubeOptions, SqlApiClient sqlClient)
        {
            if (kubeResources == null)
                throw new ArgumentNullException(nameof(kubeResources));

            if (kubeClient == null)
                throw new ArgumentNullException(nameof(kubeClient));

            if (kubeOptions == null)
                throw new ArgumentNullException(nameof(kubeOptions));

            if (sqlClient == null)
                throw new ArgumentNullException(nameof(sqlClient));
            
            KubeResources = kubeResources;
            KubeClient = kubeClient;
            KubeOptions = kubeOptions.Value;
            SqlClient = sqlClient;
        }

        /// <summary>
        ///     The actor's local message-stash facility.
        /// </summary>
        public IStash Stash { get; set; }

        /// <summary>
        ///     The <see cref="SqlApiClient"/> used to communicate with the SQL Executor API.
        /// </summary>
        SqlApiClient SqlClient { get; set; }

        /// <summary>
        ///     The Kubernetes resource factory.
        /// </summary>
        KubeResources KubeResources { get; }

        /// <summary>
        ///     The <see cref="KubeApiClient"/> used to communicate with the Kubernetes API.
        /// </summary>
        KubeApiClient KubeClient { get; set; }

        /// <summary>
        ///     Application-level Kubernetes options.
        /// </summary>
        KubernetesOptions KubeOptions { get; set; }

        /// <summary>
        ///     A reference to the <see cref="Actors.DataAccess"/> actor.
        /// </summary>
        IActorRef DataAccess { get; set; }

        /// <summary>
        ///     The Id of the target server.
        /// </summary>
        int ServerId { get; set; }

        /// <summary>
        ///     Current state (if known) from the database.
        /// </summary>
        DatabaseServer CurrentState { get; set; }

        /// <summary>
        ///     Previous state (if known) from the database.
        /// </summary>
        DatabaseServer PreviousState { get; set; }

        /// <summary>
        ///     Called when the actor is started.
        /// </summary>
        protected override void PreStart() => Become(Initializing);

        /// <summary>
        ///     Called when the actor has stopped.
        /// </summary>
        protected override void PostStop()
        {
            KubeClient.Dispose();
            SqlClient.Dispose();

            base.PostStop();
        }

        /// <summary>
        ///     Called when the actor is waiting for its <see cref="Initialize"/> message.
        /// </summary>
        void Initializing()
        {
            Receive<Initialize>(initialize =>
            {
                DataAccess = initialize.DataAccess;
                ServerId = initialize.ServerId;
                CurrentState = initialize.InitialState;

                Self.Tell(CurrentState); // Kick off initial state-management actions.

                Become(Ready);
            });

            SetReceiveTimeout(
                TimeSpan.FromSeconds(5)
            );
            Receive<ReceiveTimeout>(_ =>
            {
                Log.Error("Failed to receive Initialize message within 5 seconds of being created.");

                Context.Stop(Self);
            });
        }

        /// <summary>
        ///     Called when the actor is ready to handle requests.
        /// </summary>
        void Ready()
        {
            StopPolling();

            Log.Info("Ready to process requests for server {ServerId}.", ServerId);

            ReceiveAsync<DatabaseServer>(async databaseServer =>
            {
                Log.Debug("Received server configuration (Id:{ServerId}, Name:{ServerName}).",
                    databaseServer.Id,
                    databaseServer.Name
                );

                PreviousState = CurrentState;
                CurrentState = databaseServer;

                await UpdateServerState();
            });
            Receive<Terminated>(
                terminated => HandleTermination(terminated)
            );
        }

        /// <summary>
        ///     Called when the actor is waiting for the server's Deployment to indicate that all replicas are Available.
        /// </summary>
        void WaitForServerAvailable()
        {
            Log.Info("Waiting for server {ServerId}'s Deployment to become Available...", ServerId);

            StartPolling(Signal.PollDeployment);

            ReceiveAsync<Signal>(async signal =>
            {
                string actionDescription;
                switch (CurrentState.Action)
                {
                    case ProvisioningAction.Provision:
                    {
                        actionDescription = "Provisioning";

                        break;
                    }
                    case ProvisioningAction.Reconfigure:
                    {
                        actionDescription = "Reconfiguration";

                        break;
                    }
                    case ProvisioningAction.Deprovision:
                    {
                        actionDescription = "De-provisioning";

                        break;
                    }
                    default:
                    {
                        return;
                    }
                }

                switch (signal)
                {
                    case Signal.PollDeployment:
                    {
                        if (CurrentState.Status == ProvisioningStatus.Ready)
                        {
                            Become(Ready);

                            break;
                        }

                        DeploymentV1Beta1 deployment = await FindDeployment();
                        if (deployment == null)
                        {
                            Log.Warning("{Action} failed - cannot find Deployment for server {ServerId}.", actionDescription, ServerId);

                            FailCurrentAction();

                            Become(Ready);
                        }
                        else if (deployment.Status.AvailableReplicas == deployment.Status.Replicas)
                        {
                            Log.Info("Server {ServerID} is now available ({AvailableReplicaCount} of {ReplicaCount} replicas are marked as Available).",
                                ServerId,
                                deployment.Status.AvailableReplicas,
                                deployment.Status.Replicas
                            );

                            // We're done with the Deployment now that it's marked as Available, so we're ready to initialise the server configuration.
                            StartProvisioningPhase(ServerProvisioningPhase.Ingress);
                            Become(Ready);
                        }
                        else
                        {
                            Log.Debug("Server {ServerID} is not available yet ({AvailableReplicaCount} of {ReplicaCount} replicas are marked as Available).",
                                ServerId,
                                deployment.Status.AvailableReplicas,
                                deployment.Status.Replicas
                            );
                        }

                        break;
                    }
                    case Signal.Timeout:
                    {
                        Log.Warning("{Action} failed - timed out waiting server {ServerId}'s Deployment to become ready.", actionDescription, ServerId);

                        FailCurrentAction();

                        Become(Ready);

                        break;
                    }
                    default:
                    {
                        Unhandled(signal);

                        break;
                    }
                }
            });
            Receive<DatabaseServer>(_ =>
            {
                Log.Debug("Ignoring DatabaseServer state message (waiting for server's Deployment to become Available).'");
            });
            Receive<Terminated>(
                terminated => HandleTermination(terminated)
            );
        }

        /// <summary>
        ///     Update the server state in Kubernetes to converge with desired state.
        /// </summary>
        /// <returns>
        ///     A <see cref="Task"/> representing the operation.
        /// </returns>
        async Task UpdateServerState()
        {
            switch (CurrentState.Action)
            {
                case ProvisioningAction.Provision:
                {
                    Log.Info("Provisioning server {ServerId} ({Phase})...", CurrentState.Id, CurrentState.Phase);

                    try
                    {
                        await ProvisionServer();
                    }
                    catch (HttpRequestException<StatusV1> provisioningFailed)
                    {
                        Log.Error(provisioningFailed, "Failed to provision server {ServerId} ({Reason}): {ErrorMessage}",
                            CurrentState.Id,
                            provisioningFailed.Response.Reason,
                            provisioningFailed.Response.Message
                        );

                        FailCurrentAction();

                        return;
                    }
                    catch (Exception provisioningFailed)
                    {
                        Log.Error(provisioningFailed, "Failed to provision server {ServerId}.",
                            CurrentState.Id
                        );

                        FailCurrentAction();

                        return;
                    }

                    break;
                }
                case ProvisioningAction.Reconfigure:
                {
                    Log.Info("Reconfiguring server {ServerId} ({Phase})...", CurrentState.Id, CurrentState.Phase);

                    try
                    {
                        await ReconfigureServer();
                    }
                    catch (HttpRequestException<StatusV1> reconfigurationFailed)
                    {
                        Log.Error(reconfigurationFailed, "Failed to reconfigure server {ServerId} ({Reason}): {ErrorMessage}",
                            CurrentState.Id,
                            reconfigurationFailed.Response.Reason,
                            reconfigurationFailed.Response.Message
                        );

                        FailCurrentAction();

                        return;
                    }
                    catch (Exception reconfigurationFailed)
                    {
                        Log.Error(reconfigurationFailed, "Failed to reconfigure server {ServerId}.",
                            CurrentState.Id
                        );

                        FailCurrentAction();

                        return;
                    }

                    break;
                }
                case ProvisioningAction.Deprovision:
                {
                    Log.Info("De-provisioning server {ServerId} ({Phase})...", CurrentState.Id, CurrentState.Phase);

                    try
                    {
                        await DeprovisionServer();
                    }
                    catch (HttpRequestException<StatusV1> deprovisioningFailed)
                    {
                        Log.Error(deprovisioningFailed, "Failed to de-provision server {ServerId} ({Reason}): {ErrorMessage}",
                            CurrentState.Id,
                            deprovisioningFailed.Response.Reason,
                            deprovisioningFailed.Response.Message
                        );

                        FailCurrentAction();

                        return;
                    }
                    catch (Exception deprovisioningFailed)
                    {
                        Log.Error(deprovisioningFailed, "Failed to de-provision server {ServerId}.",
                            CurrentState.Id
                        );

                        FailCurrentAction();

                        return;
                    }

                    // Like tears in rain, time to die.
                    Context.Stop(Self);

                    return;
                }
            }

            await UpdateServerIngressDetails();

            if (CurrentState.Status == ProvisioningStatus.Ready)
                UpdateDatabaseState();
        }

        /// <summary>
        ///     Update the server's databases to converge with desired state.
        /// </summary>
        void UpdateDatabaseState()
        {
            foreach (DatabaseInstance database in CurrentState.Databases)
            {
                Log.Debug("Server configuration includes database {DatabaseName} (Id:{ServerId}).",
                    database.Name,
                    database.Id
                );

                // Hook up reverse-navigation property because TenantDatabaseManager will need server connection info.
                database.DatabaseServer = CurrentState;

                IActorRef databaseManager;
                if (!_databaseManagers.TryGetValue(database.Id, out databaseManager))
                {
                    databaseManager = Context.ActorOf(
                        Context.DI().Props<TenantDatabaseManager>(),
                        name: TenantDatabaseManager.ActorName(database.Id)
                    );
                    Context.Watch(databaseManager);
                    _databaseManagers.Add(database.Id, databaseManager);

                    databaseManager.Tell(new TenantDatabaseManager.Initialize(
                        serverManager: Self,
                        dataAccess: DataAccess,
                        initialState: database
                    ));

                    Log.Info("Created TenantDatabaseManager {ActorName} for database {DatabaseId} in server {ServerId} (Tenant:{TenantId}).",
                        databaseManager.Path.Name,
                        database.Id,
                        CurrentState.Id,
                        CurrentState.TenantId
                    );
                }
                else
                {
                    Log.Debug("Notifying TenantDatabaseManager {ActorName} of current configuration for database {DatabaseId}.", databaseManager.Path.Name, database.Id);
                    databaseManager.Tell(database);
                }
            }
        }

        /// <summary>
        ///     Handle the termination of a watched actor.
        /// </summary>
        /// <param name="terminated">
        ///     A <see cref="Terminated"/> message representing the termination.
        /// </param>
        void HandleTermination(Terminated terminated)
        {
            if (terminated == null)
                throw new ArgumentNullException(nameof(terminated));
            
            foreach ((int databaseId, IActorRef databaseManager) in _databaseManagers)
            {
                if (Equals(terminated.ActorRef, databaseManager))
                {
                    Log.Info("DatabaseManager for database {DatabaseId} in server {ServerId} has terminated.",
                        databaseId,
                        ServerId
                    );

                    _databaseManagers.Remove(databaseId); // Database manager terminated.

                    return;
                }
            }

            Unhandled(terminated); // DeathPactException
        }

        /// <summary>
        ///     Deploy an instance of SQL Server.
        /// </summary>
        /// <returns>
        ///     A <see cref="Task"/> representing the operation.
        /// </returns>
        async Task ProvisionServer()
        {
            switch (CurrentState.Phase)
            {
                case ServerProvisioningPhase.None:
                {
                    goto case ServerProvisioningPhase.Instance;
                }
                case ServerProvisioningPhase.Instance:
                {
                    StartProvisioningPhase(ServerProvisioningPhase.Instance);

                    await EnsureDeploymentPresent();
                    
                    goto case ServerProvisioningPhase.Network;
                }
                case ServerProvisioningPhase.Network:
                {
                    StartProvisioningPhase(ServerProvisioningPhase.Network);

                    await EnsureInternalServicePresent();

                    goto case ServerProvisioningPhase.Monitoring;
                }
                case ServerProvisioningPhase.Monitoring:
                {
                    StartProvisioningPhase(ServerProvisioningPhase.Monitoring);

                    await EnsureServiceMonitorPresent();

                    Become(WaitForServerAvailable); // We can't proceed until the deployment becomes available.

                    break;
                }
                case ServerProvisioningPhase.Configuration:
                {
                    StartProvisioningPhase(ServerProvisioningPhase.Configuration);

                    await InitialiseServerConfiguration();

                    goto case ServerProvisioningPhase.Ingress;
                }
                case ServerProvisioningPhase.Ingress:
                {
                    StartProvisioningPhase(ServerProvisioningPhase.Ingress);

                    await EnsureExternalServicePresent();

                    break;
                }

                // Provisioning completes when the server has valid Ingress details.
            }
        }

        /// <summary>
        ///     Reconfigure / repair an instance of SQL Server.
        /// </summary>
        /// <returns>
        ///     A <see cref="Task"/> representing the operation.
        /// </returns>
        async Task ReconfigureServer()
        {
            switch (CurrentState.Phase)
            {
                case ServerProvisioningPhase.None:
                {
                    goto case ServerProvisioningPhase.Instance;
                }
                case ServerProvisioningPhase.Instance:
                {
                    StartReconfigurationPhase(ServerProvisioningPhase.Instance);

                    await EnsureDeploymentPresent();
                    
                    goto case ServerProvisioningPhase.Network;
                }
                case ServerProvisioningPhase.Network:
                {
                    StartReconfigurationPhase(ServerProvisioningPhase.Network);

                    await EnsureInternalServicePresent();

                    goto case ServerProvisioningPhase.Monitoring;
                }
                case ServerProvisioningPhase.Monitoring:
                {
                    StartProvisioningPhase(ServerProvisioningPhase.Monitoring);

                    await EnsureServiceMonitorPresent();

                    Become(WaitForServerAvailable); // We can't proceed until the deployment becomes available.

                    break;
                }
                case ServerProvisioningPhase.Configuration:
                {
                    StartReconfigurationPhase(ServerProvisioningPhase.Configuration);
                    
                    await InitialiseServerConfiguration();

                    goto case ServerProvisioningPhase.Ingress;
                }
                case ServerProvisioningPhase.Ingress:
                {
                    StartReconfigurationPhase(ServerProvisioningPhase.Ingress);

                    await EnsureExternalServicePresent();

                    break;
                }
                
                // Reconfiguration completes when the server has valid Ingress details.
            }
        }

        /// <summary>
        ///     Destroy an instance of SQL Server.
        /// </summary>
        /// <returns>
        ///     A <see cref="Task"/> representing the operation.
        /// </returns>
        async Task DeprovisionServer()
        {
            switch (CurrentState.Phase)
            {
                case ServerProvisioningPhase.None:
                {
                    goto case ServerProvisioningPhase.Instance;
                }
                case ServerProvisioningPhase.Instance:
                {
                    StartDeprovisioningPhase(ServerProvisioningPhase.Instance);

                    await EnsureDeploymentAbsent();
                    
                    goto case ServerProvisioningPhase.Network;
                }
                case ServerProvisioningPhase.Network:
                {
                    StartDeprovisioningPhase(ServerProvisioningPhase.Network);
                    
                    await EnsureInternalServiceAbsent();

                    goto case ServerProvisioningPhase.Monitoring;
                }
                case ServerProvisioningPhase.Monitoring:
                {
                    StartDeprovisioningPhase(ServerProvisioningPhase.Monitoring);
                    
                    await EnsureServiceMonitorAbsent();

                    goto case ServerProvisioningPhase.Ingress;
                }
                case ServerProvisioningPhase.Ingress:
                {
                    StartDeprovisioningPhase(ServerProvisioningPhase.Ingress);

                    await EnsureExternalServiceAbsent();

                    goto case ServerProvisioningPhase.Done;
                }
                case ServerProvisioningPhase.Done:
                {
                    CompleteCurrentAction();

                    break;
                }
            }
        }

        /// <summary>
        ///     Find the server's associated Deployment (if it exists).
        /// </summary>
        /// <returns>
        ///     The Deployment, or <c>null</c> if it was not found.
        /// </returns>
        async Task<DeploymentV1Beta1> FindDeployment()
        {
            List<DeploymentV1Beta1> matchingDeployments = await KubeClient.DeploymentsV1Beta1.List(
                 labelSelector: $"cloud.dimensiondata.daas.server-id = {CurrentState.Id}"
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
        async Task<ServiceV1> FindInternalService()
        {
            List<ServiceV1> matchingServices = await KubeClient.ServicesV1.List(
                labelSelector: $"cloud.dimensiondata.daas.server-id = {CurrentState.Id},cloud.dimensiondata.daas.service-type = internal"
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
        async Task<PrometheusServiceMonitorV1> FindServiceMonitor()
        {
            List<PrometheusServiceMonitorV1> matchingServices = await KubeClient.PrometheusServiceMonitorsV1.List(
                labelSelector: $"cloud.dimensiondata.daas.server-id = {CurrentState.Id},cloud.dimensiondata.daas.monitor-type = sql-server"
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
        async Task<ServiceV1> FindExternalService()
        {
            List<ServiceV1> matchingServices = await KubeClient.ServicesV1.List(
                labelSelector: $"cloud.dimensiondata.daas.server-id = {CurrentState.Id},cloud.dimensiondata.daas.service-type = external"
            );
            if (matchingServices.Count == 0)
                return null;

            return matchingServices[matchingServices.Count - 1];
        }

        /// <summary>
        ///     Ensure that a Deployment resource exists for the specified database server.
        /// </summary>
        /// <returns>
        ///     The Deployment resource, as a <see cref="DeploymentV1Beta1"/>.
        /// </returns>
        async Task<DeploymentV1Beta1> EnsureDeploymentPresent()
        {
            DeploymentV1Beta1 existingDeployment = await FindDeployment();
            if (existingDeployment != null)
            {
                Log.Info("Found existing deployment {DeploymentName} for server {ServerId}.",
                    existingDeployment.Metadata.Name,
                    CurrentState.Id
                );

                return existingDeployment;
            }

            Log.Info("Creating deployment for server {ServerId}...",
                CurrentState.Id
            );

            DeploymentV1Beta1 createdDeployment = await KubeClient.DeploymentsV1Beta1.Create(
                KubeResources.Deployment(CurrentState)
            );

            Log.Info("Successfully created deployment {DeploymentName} for server {ServerId}.",
                createdDeployment.Metadata.Name,
                CurrentState.Id
            );

            return createdDeployment;
        }

        /// <summary>
        ///     Ensure that a Deployment resource does not exist for the specified database server.
        /// </summary>
        /// <returns>
        ///     <c>true</c>, if the controller is now absent; otherwise, <c>false</c>.
        /// </returns>
        async Task<bool> EnsureDeploymentAbsent()
        {
            DeploymentV1Beta1 controller = await FindDeployment();
            if (controller == null)
                return true;

            Log.Info("Deleting deployment {DeploymentName} for server {ServerId}...",
                controller.Metadata.Name,
                CurrentState.Id
            );

            try
            {
                await KubeClient.DeploymentsV1Beta1.Delete(
                    name: controller.Metadata.Name,
                    propagationPolicy: DeletePropagationPolicy.Background
                );
            }
            catch (HttpRequestException<StatusV1> deleteFailed)
            {
                Log.Error("Failed to delete deployment {DeploymentName} for server {ServerId} (Message:{FailureMessage}, Reason:{FailureReason}).",
                    controller.Metadata.Name,
                    CurrentState.Id,
                    deleteFailed.Response.Message,
                    deleteFailed.Response.Reason
                );

                return false;
            }

            Log.Info("Deleted deployment {DeploymentName} for server {ServerId}.",
                controller.Metadata.Name,
                CurrentState.Id
            );

            return true;
        }

        /// <summary>
        ///     Ensure that an internally-facing Service resource exists for the specified database server.
        /// </summary>
        /// <returns>
        ///     The Service resource, as a <see cref="ServiceV1"/>.
        /// </returns>
        async Task EnsureInternalServicePresent()
        {
            ServiceV1 existingInternalService = await FindInternalService();
            if (existingInternalService == null)
            {
                Log.Info("Creating internal service for server {ServerId}...",
                    CurrentState.Id
                );

                ServiceV1 createdService = await KubeClient.ServicesV1.Create(
                    KubeResources.InternalService(CurrentState)
                );

                Log.Info("Successfully created internal service {ServiceName} for server {ServerId}.",
                    createdService.Metadata.Name,
                    CurrentState.Id
                );
            }
            else
            {
                Log.Info("Found existing internal service {ServiceName} for server {ServerId}.",
                    existingInternalService.Metadata.Name,
                    CurrentState.Id
                );
            }
        }

        /// <summary>
        ///     Ensure that an internally-facing Service resource does not exist for the specified database server.
        /// </summary>
        async Task EnsureInternalServiceAbsent()
        {
            ServiceV1 existingInternalService = await FindInternalService();
            if (existingInternalService != null)
            {
                Log.Info("Deleting internal service {ServiceName} for server {ServerId}...",
                    existingInternalService.Metadata.Name,
                    CurrentState.Id
                );

                StatusV1 result = await KubeClient.ServicesV1.Delete(
                    name: existingInternalService.Metadata.Name
                );

                if (result.Status != "Success" && result.Reason != "NotFound")
                {
                    Log.Error("Failed to delete internal service {ServiceName} for server {ServerId} (Message:{FailureMessage}, Reason:{FailureReason}).",
                        existingInternalService.Metadata.Name,
                        CurrentState.Id,
                        result.Message,
                        result.Reason
                    );
                }

                Log.Info("Deleted internal service {ServiceName} for server {ServerId}.",
                    existingInternalService.Metadata.Name,
                    CurrentState.Id
                );
            }
        }

        /// <summary>
        ///     Ensure that a ServiceMonitor resource exists for the specified database server.
        /// </summary>
        /// <returns>
        ///     The Service resource, as a <see cref="ServiceV1"/>.
        /// </returns>
        async Task EnsureServiceMonitorPresent()
        {
            PrometheusServiceMonitorV1 existingServiceMonitor = await FindServiceMonitor();
            if (existingServiceMonitor == null)
            {
                Log.Info("Creating service monitor for server {ServerId}...",
                    CurrentState.Id
                );

                PrometheusServiceMonitorV1 createdService = await KubeClient.PrometheusServiceMonitorsV1.Create(
                    KubeResources.ServiceMonitor(CurrentState)
                );

                Log.Info("Successfully created service monitor {ServiceName} for server {ServerId}.",
                    createdService.Metadata.Name,
                    CurrentState.Id
                );
            }
            else
            {
                Log.Info("Found existing service monitor {ServiceName} for server {ServerId}.",
                    existingServiceMonitor.Metadata.Name,
                    CurrentState.Id
                );
            }
        }

        /// <summary>
        ///     Ensure that a ServiceMonitor resource does not exist for the specified database server.
        /// </summary>
        async Task EnsureServiceMonitorAbsent()
        {
            PrometheusServiceMonitorV1 existingServiceMonitor = await FindServiceMonitor();
            if (existingServiceMonitor != null)
            {
                Log.Info("Deleting service monitor {ServiceName} for server {ServerId}...",
                    existingServiceMonitor.Metadata.Name,
                    CurrentState.Id
                );

                StatusV1 result = await KubeClient.PrometheusServiceMonitorsV1.Delete(
                    name: existingServiceMonitor.Metadata.Name
                );

                if (result.Status != "Success" && result.Reason != "NotFound")
                {
                    Log.Error("Failed to delete service monitor {ServiceName} for server {ServerId} (Message:{FailureMessage}, Reason:{FailureReason}).",
                        existingServiceMonitor.Metadata.Name,
                        CurrentState.Id,
                        result.Message,
                        result.Reason
                    );
                }

                Log.Info("Deleted service monitor {ServiceName} for server {ServerId}.",
                    existingServiceMonitor.Metadata.Name,
                    CurrentState.Id
                );
            }
        }

        /// <summary>
        ///     Ensure that an externally-facing Service resource exists for the specified database server.
        /// </summary>
        /// <returns>
        ///     A <see cref="Task"/> representing the operation.
        /// </returns>
        async Task EnsureExternalServicePresent()
        {
            ServiceV1 existingExternalService = await FindExternalService();
            if (existingExternalService == null)
            {
                Log.Info("Creating external service for server {ServerId}...",
                    CurrentState.Id
                );

                ServiceV1 createdService = await KubeClient.ServicesV1.Create(
                    KubeResources.ExternalService(CurrentState)
                );

                Log.Info("Successfully created external service {ServiceName} for server {ServerId}.",
                    createdService.Metadata.Name,
                    CurrentState.Id
                );
            }
            else
            {
                Log.Info("Found existing external service {ServiceName} for server {ServerId}.",
                    existingExternalService.Metadata.Name,
                    CurrentState.Id
                );
            }
        }

        /// <summary>
        ///     Ensure that an externally-facing Service resource does not exist for the specified database server.
        /// </summary>
        async Task EnsureExternalServiceAbsent()
        {
            ServiceV1 existingExternalService = await FindExternalService();
            if (existingExternalService != null)
            {
                Log.Info("Deleting external service {ServiceName} for server {ServerId}...",
                    existingExternalService.Metadata.Name,
                    CurrentState.Id
                );

                StatusV1 result = await KubeClient.ServicesV1.Delete(
                    name: existingExternalService.Metadata.Name
                );

                if (result.Status != "Success" && result.Reason != "NotFound")
                {
                    Log.Error("Failed to delete external service {ServiceName} for server {ServerId} (Message:{FailureMessage}, Reason:{FailureReason}).",
                        existingExternalService.Metadata.Name,
                        CurrentState.Id,
                        result.Message,
                        result.Reason
                    );
                }

                Log.Info("Deleted external service {ServiceName} for server {ServerId}.",
                    existingExternalService.Metadata.Name,
                    CurrentState.Id
                );
            }
        }

        /// <summary>
        ///     Update ingress details for the database server.
        /// </summary>
        /// <returns>
        ///     A <see cref="Task"/> representing the operation.
        /// </returns>
        async Task UpdateServerIngressDetails()
        {
            int? externalPort = await KubeClient.GetServerPublicPort(CurrentState);
            if (externalPort != null)
            {
                if (KubeOptions.ClusterPublicFQDN != CurrentState.PublicFQDN || externalPort != CurrentState.PublicPort)
                {
                    Log.Info("Server {ServerName} is accessible at {ClusterPublicFQDN}:{PublicPortPort}",
                        CurrentState.Name,
                        KubeOptions.ClusterPublicFQDN,
                        externalPort.Value
                    );

                    DataAccess.Tell(
                        new ServerIngressChanged(ServerId, KubeOptions.ClusterPublicFQDN, externalPort)
                    );

                    // Capture current ingress details to enable subsequent provisioning actions (if any).
                    CurrentState.PublicFQDN = KubeOptions.ClusterPublicFQDN;
                    CurrentState.PublicPort = externalPort;
                }

                if (CurrentState.Phase == ServerProvisioningPhase.Ingress)
                    CompleteCurrentAction();
            }
            else
            {
                Log.Debug("Cannot determine public port for server {ServerName}.", CurrentState.Name);

                if (CurrentState.PublicFQDN != null)
                {
                    DataAccess.Tell(
                        new ServerIngressChanged(ServerId, publicFQDN: null, publicPort: null)
                    );
                }
            }
        }

        /// <summary>
        ///     Execute T-SQL to initialise the server configuration.
        /// </summary>
        /// <returns>
        ///     A <see cref="Task"/> representing the operation.
        /// </returns>
        async Task InitialiseServerConfiguration()
        {
            Log.Info("Initialising configuration for server {ServerId}...", ServerId);
            
            CommandResult commandResult = await SqlClient.ExecuteCommand(
                serverId: CurrentState.Id,
                databaseId: SqlApiClient.MasterDatabaseId,
                sql: ManagementSql.ConfigureServerMemory(maxMemoryMB: 500 * 1024),
                executeAsAdminUser: true
            );

            for (int messageIndex = 0; messageIndex < commandResult.Messages.Count; messageIndex++)
            {
                Log.Info("T-SQL message [{MessageIndex}] from server {ServerId}: {TSqlMessage}",
                    messageIndex,
                    ServerId,
                    commandResult.Messages[messageIndex]
                );
            }

            if (!commandResult.Success)
            {
                foreach (SqlError error in commandResult.Errors)
                {
                    Log.Warning("Error encountered while initialising configuration for server {ServerId} ({ErrorKind}: {ErrorMessage})",
                        ServerId,
                        error.Kind,
                        error.Message
                    );
                }

                throw new SqlExecutionException($"One or more errors were encountered while configuring server (Id: {ServerId}).",
                    serverId: CurrentState.Id,
                    databaseId: SqlApiClient.MasterDatabaseId,
                    sqlMessages: commandResult.Messages,
                    sqlErrors: commandResult.Errors
                );
            }

            Log.Info("Configuration initialised for server {ServerId}.", ServerId);
        }

        /// <summary>
        ///     Start periodic polling using the specified <see cref="Signal"/>.
        /// </summary>
        /// <param name="pollSignal">
        ///     A <see cref="Signal"/> value indicating the type of polling to perform.
        /// </param>
        void StartPolling(Signal pollSignal)
        {
            _timeoutCancellation?.Cancel();
            _pollCancellation?.Cancel();

            _pollCancellation = Context.System.Scheduler.ScheduleTellRepeatedlyCancelable(
                initialDelay: TimeSpan.FromSeconds(1),
                interval: PollPeriod,
                receiver: Self,
                message: pollSignal,
                sender: Self
            );
            _timeoutCancellation = Context.System.Scheduler.ScheduleTellOnceCancelable(
                delay: PollTimeout,
                receiver: Self,
                message: pollSignal,
                sender: Self
            );
        }

        /// <summary>
        ///     Stop periodic polling.
        /// </summary>
        void StopPolling()
        {
            if (_timeoutCancellation != null)
            {
                _timeoutCancellation.Cancel();
                _timeoutCancellation = null;
            }
            if (_pollCancellation != null)
            {
                _pollCancellation.Cancel();
                _pollCancellation = null;
            }
        }

        /// <summary>
        ///     Set and persist the current provisioning phase.
        /// </summary>
        /// <param name="phase">
        ///     The current provisioning phase.
        /// </param>
        void StartProvisioningPhase(ServerProvisioningPhase phase)
        {
            CurrentState.Phase = phase;
            DataAccess.Tell(
                new ServerProvisioning(ServerId, phase)
            );

            Log.Info("Starting provisioning phase {Phase} for server {ServerId}.", phase, ServerId);
        }

        /// <summary>
        ///     Set and persist the current reconfiguration phase.
        /// </summary>
        /// <param name="phase">
        ///     The current reconfiguration phase.
        /// </param>
        void StartReconfigurationPhase(ServerProvisioningPhase phase)
        {
            CurrentState.Phase = phase;
            DataAccess.Tell(
                new ServerReconfiguring(ServerId, phase)
            );

            Log.Info("Starting reconfiguration phase {Phase} for server {ServerId}.", phase, ServerId);
        }

        /// <summary>
        ///     Set and persist the current de-provisioning phase.
        /// </summary>
        /// <param name="phase">
        ///     The current de-provisioning phase.
        /// </param>
        void StartDeprovisioningPhase(ServerProvisioningPhase phase)
        {
            CurrentState.Phase = phase;
            DataAccess.Tell(
                new ServerDeprovisioning(ServerId, phase)
            );

            Log.Info("Started de-provisioning phase {Phase} for server {ServerId}.", phase, ServerId);
        }

        /// <summary>
        ///     Fail the current provision / reconfigure / de-provision action.
        /// </summary>
        void FailCurrentAction()
        {
            CurrentState.Status = ProvisioningStatus.Error;
            switch (CurrentState.Action)
            {
                case ProvisioningAction.Provision:
                {
                    DataAccess.Tell(
                        new ServerProvisioningFailed(ServerId)
                    );

                    break;
                }
                case ProvisioningAction.Reconfigure:
                {
                    DataAccess.Tell(
                        new ServerReconfigurationFailed(ServerId)
                    );

                    break;
                }
                case ProvisioningAction.Deprovision:
                {
                    DataAccess.Tell(
                        new ServerDeprovisioningFailed(ServerId)
                    );

                    break;
                }
            }

            CurrentState.Phase = ServerProvisioningPhase.None;
        }

        /// <summary>
        ///     Complete the current provision / reconfigure / de-provision action.
        /// </summary>
        void CompleteCurrentAction()
        {
            switch (CurrentState.Action)
            {
                case ProvisioningAction.Provision:
                {
                    DataAccess.Tell(
                        new ServerProvisioned(ServerId)
                    );

                    break;
                }
                case ProvisioningAction.Reconfigure:
                {
                    DataAccess.Tell(
                        new ServerReconfigured(ServerId)
                    );

                    break;
                }
                case ProvisioningAction.Deprovision:
                {
                    DataAccess.Tell(
                        new ServerDeprovisioned(ServerId)
                    );

                    break;
                }
            }

            Log.Info("Completed action {Action} for server {ServerId}.",
                CurrentState.Action,
                CurrentState.Id
            );
        }

        /// <summary>
        ///     Initialise the actor.
        /// </summary>
        public class Initialize
        {
            /// <summary>
            ///     Create a new <see cref="Initialize"/> message.
            /// </summary>
            /// <param name="initialState">
            ///     A <see cref="DatabaseServer"/> representing the actor's initial state.
            /// </param>
            /// <param name="dataAccess">
            ///     A reference to the <see cref="Actors.DataAccess"/> actor.
            /// </param>
            public Initialize(DatabaseServer initialState, IActorRef dataAccess)
            {
                if (initialState == null)
                    throw new ArgumentNullException(nameof(initialState));
                
                if (dataAccess == null)
                    throw new ArgumentNullException(nameof(dataAccess));
                
                InitialState = initialState;
                DataAccess = dataAccess;
            }

            /// <summary>
            ///     The Id of the target server.
            /// </summary>
            public int ServerId => InitialState.Id;

            /// <summary>
            ///     A <see cref="DatabaseServer"/> representing the actor's initial state.
            /// </summary>
            public DatabaseServer InitialState { get; }

            /// <summary>
            ///     A reference to the <see cref="Actors.DataAccess"/> actor.
            /// </summary>
            public IActorRef DataAccess { get; }
        }

        /// <summary>
        ///     Signal messages used to tell the <see cref="TenantServerManager"/> to perform an action.
        /// </summary>
        public enum Signal
        {
            /// <summary>
            ///     Poll the status of the server's Deployment.
            /// </summary>
            PollDeployment = 1,

            /// <summary>
            ///     The current polling operation timed out.
            /// </summary>
            Timeout = 500
        }

        /// <summary>
        ///     Get the name of the <see cref="TenantServerManager"/> actor for the specified tenant.
        /// </summary>
        /// <param name="tenantId">
        ///     The tenant Id.
        /// </param>
        /// <returns>
        ///     The actor name.
        /// </returns>
        public static string ActorName(int tenantId) => $"server-manager.{tenantId}";
    }
}
