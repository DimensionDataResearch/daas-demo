using Akka;
using Akka.Actor;
using HTTPlease;
using Microsoft.EntityFrameworkCore;
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

    /// <summary>
    ///     Actor that represents a tenant's database server and manages its life-cycle.
    /// </summary>
    /// <remarks>
    ///     Management of the server's databases is delegated to a child <see cref="TenantDatabaseManager"/> actor.
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
        ///     The Id of the target server.
        /// </summary>
        readonly int _serverId;

        /// <summary>
        ///     A reference to the <see cref="DataAccess"/> actor.
        /// </summary>
        readonly IActorRef _dataAccess;

        /// <summary>
        ///     The <see cref="KubeApiClient"/> used to communicate with the Kubernetes API.
        /// </summary>
        readonly KubeApiClient _kubeClient;

        /// <summary>
        ///     The <see cref="SqlApiClient"/> used to communicate with the SQL executor API.
        /// </summary>
        readonly SqlApiClient _sqlClient;

        /// <summary>
        ///     The Kubernetes cluster's fully-qualified domain name.
        /// </summary>
        string _clusterPublicDomainName;

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
        /// <param name="serverId">
        ///     The Id of the target server.
        /// </param>
        /// <param name="databaseWatcher">
        ///     A reference to the <see cref="DataAccess"/> actor.
        /// </param>
        public TenantServerManager(int serverId, IActorRef databaseWatcher)
        {
            if (databaseWatcher == null)
                throw new ArgumentNullException(nameof(databaseWatcher));

            _serverId = serverId;
            _dataAccess = databaseWatcher;

            _kubeClient = CreateKubeApiClient();
            _sqlClient = CreateSqlApiClient();
        }

        /// <summary>
        ///     The actor's local message-stash facility.
        /// </summary>
        public IStash Stash { get; set; }

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
        protected override void PreStart()
        {
            _clusterPublicDomainName = Context.System.Settings.Config.GetString("daas.kube.cluster-public-fqdn");

            Become(Ready);
        }

        /// <summary>
        ///     Called when the actor has stopped.
        /// </summary>
        protected override void PostStop()
        {
            _kubeClient.Dispose();
            _sqlClient.Dispose();

            base.PostStop();
        }

        /// <summary>
        ///     Called when the actor is ready to handle requests.
        /// </summary>
        void Ready()
        {
            StopPolling();

            Log.Info("Ready to process requests for server {ServerId}.", _serverId);

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
        ///     Called when the actor is waiting for the server's ReplicationController to indicate that all replicas are Available.
        /// </summary>
        void WaitForServerAvailable()
        {
            Log.Info("Waiting for server {ServerId}'s ReplicationController to become Available...", _serverId);

            StartPolling(Signal.PollReplicationController);

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
                    case Signal.PollReplicationController:
                    {
                        if (CurrentState.Status == ProvisioningStatus.Ready)
                        {
                            Become(Ready);

                            break;
                        }

                        ReplicationControllerV1 replicationController = await FindReplicationController();
                        if (replicationController == null)
                        {
                            Log.Warning("{Action} failed - cannot find ReplicationController for server {ServerId}.", actionDescription, _serverId);

                            FailCurrentAction();

                            Become(Ready);
                        }
                        else if (replicationController.Status.AvailableReplicas == replicationController.Status.Replicas)
                        {
                            Log.Info("Server {ServerID} is now available ({AvailableReplicaCount} of {ReplicaCount} replicas are marked as Available).",
                                _serverId,
                                replicationController.Status.AvailableReplicas,
                                replicationController.Status.Replicas
                            );

                            // We're done with the ReplicationController now that it's marked as Available, so we're ready to initialise the server configuration.
                            StartProvisioningPhase(ServerProvisioningPhase.Ingress);
                            Become(Ready);
                        }
                        else
                        {
                            Log.Debug("Server {ServerID} is not available yet ({AvailableReplicaCount} of {ReplicaCount} replicas are marked as Available).",
                                _serverId,
                                replicationController.Status.AvailableReplicas,
                                replicationController.Status.Replicas
                            );
                        }

                        break;
                    }
                    case Signal.Timeout:
                    {
                        Log.Warning("{Action} failed - timed out waiting server {ServerId}'s ReplicationController to become ready.", actionDescription, _serverId);

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
                Log.Debug("Ignoring DatabaseServer state message (waiting for server's ReplicationController to become Available).'");
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

                IActorRef databaseManager;
                if (!_databaseManagers.TryGetValue(database.Id, out databaseManager))
                {
                    databaseManager = Context.ActorOf(
                        Props.Create(() => new TenantDatabaseManager(Self, _dataAccess)),
                        name: TenantDatabaseManager.ActorName(database.Id)
                    );
                    Context.Watch(databaseManager);
                    _databaseManagers.Add(database.Id, databaseManager);

                    Log.Info("Created TenantDatabaseManager {ActorName} for database {DatabaseId} in server {ServerId} (Tenant:{TenantId}).",
                        databaseManager.Path.Name,
                        database.Id,
                        CurrentState.Id,
                        CurrentState.TenantId
                    );
                }

                // Hook up reverse-navigation property because TenantDatabaseManager will need server connection info.
                database.DatabaseServer = CurrentState;

                databaseManager.Tell(database);
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
                        _serverId
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

                    await EnsureReplicationControllerPresent();
                    
                    goto case ServerProvisioningPhase.Network;
                }
                case ServerProvisioningPhase.Network:
                {
                    StartProvisioningPhase(ServerProvisioningPhase.Network);

                    await EnsureInternalServicePresent();

                    Become(WaitForServerAvailable); // We can't proceed until the replication controller becomes available.

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

                    await EnsureReplicationControllerPresent();
                    
                    goto case ServerProvisioningPhase.Network;
                }
                case ServerProvisioningPhase.Network:
                {
                    StartReconfigurationPhase(ServerProvisioningPhase.Network);

                    await EnsureInternalServicePresent();

                    Become(WaitForServerAvailable); // We can't proceed until the replication controller becomes available.

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

                    await EnsureReplicationControllerAbsent();
                    
                    goto case ServerProvisioningPhase.Network;
                }
                case ServerProvisioningPhase.Network:
                {
                    StartDeprovisioningPhase(ServerProvisioningPhase.Network);
                    
                    await EnsureInternalServiceAbsent();

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
        ///     Find the server's associated ReplicationController (if it exists).
        /// </summary>
        /// <returns>
        ///     The ReplicationController, or <c>null</c> if it was not found.
        /// </returns>
        async Task<ReplicationControllerV1> FindReplicationController()
        {
            List<ReplicationControllerV1> matchingControllers = await _kubeClient.ReplicationControllersV1.List(
                 labelSelector: $"cloud.dimensiondata.daas.server-id = {CurrentState.Id}"
             );

            if (matchingControllers.Count == 0)
                return null;

            return matchingControllers[matchingControllers.Count - 1];
        }

        /// <summary>
        ///     Find the server's associated internally-facing Service (if it exists).
        /// </summary>
        /// <returns>
        ///     The Service, or <c>null</c> if it was not found.
        /// </returns>
        async Task<ServiceV1> FindInternalService()
        {
            List<ServiceV1> matchingServices = await _kubeClient.ServicesV1.List(
                labelSelector: $"cloud.dimensiondata.daas.server-id = {CurrentState.Id},cloud.dimensiondata.daas.service-type = internal"
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
            List<ServiceV1> matchingServices = await _kubeClient.ServicesV1.List(
                labelSelector: $"cloud.dimensiondata.daas.server-id = {CurrentState.Id},cloud.dimensiondata.daas.service-type = external"
            );
            if (matchingServices.Count == 0)
                return null;

            return matchingServices[matchingServices.Count - 1];
        }

        /// <summary>
        ///     Ensure that a ReplicationController resource exists for the specified database server.
        /// </summary>
        /// <returns>
        ///     The ReplicationController resource, as a <see cref="ReplicationControllerV1"/>.
        /// </returns>
        async Task<ReplicationControllerV1> EnsureReplicationControllerPresent()
        {
            ReplicationControllerV1 existingController = await FindReplicationController();
            if (existingController != null)
            {
                Log.Info("Found existing replication controller {ReplicationControllerName} for server {ServerId}.",
                    existingController.Metadata.Name,
                    CurrentState.Id
                );

                return existingController;
            }

            Log.Info("Creating replication controller for server {ServerId}...",
                CurrentState.Id
            );

            ReplicationControllerV1 createdController = await _kubeClient.ReplicationControllersV1.Create(
                KubeResources.ReplicationController(CurrentState,
                    imageName: Context.System.Settings.Config.GetString("daas.kube.sql-image-name"),
                    dataVolumeClaimName: Context.System.Settings.Config.GetString("daas.kube.volume-claim-name")
                )
            );

            Log.Info("Successfully created replication controller {ReplicationControllerName} for server {ServerId}.",
                createdController.Metadata.Name,
                CurrentState.Id
            );

            return createdController;
        }

        /// <summary>
        ///     Ensure that a ReplicationController resource does not exist for the specified database server.
        /// </summary>
        /// <returns>
        ///     <c>true</c>, if the controller is now absent; otherwise, <c>false</c>.
        /// </returns>
        async Task<bool> EnsureReplicationControllerAbsent()
        {
            ReplicationControllerV1 controller = await FindReplicationController();
            if (controller == null)
                return true;

            Log.Info("Deleting replication controller {ControllerName} for server {ServerId}...",
                controller.Metadata.Name,
                CurrentState.Id
            );

            try
            {
                await _kubeClient.ReplicationControllersV1.Delete(
                    name: controller.Metadata.Name,
                    propagationPolicy: DeletePropagationPolicy.Background
                );
            }
            catch (HttpRequestException<StatusV1> deleteFailed)
            {
                Log.Error("Failed to delete replication controller {ControllerName} for server {ServerId} (Message:{FailureMessage}, Reason:{FailureReason}).",
                    controller.Metadata.Name,
                    CurrentState.Id,
                    deleteFailed.Response.Message,
                    deleteFailed.Response.Reason
                );

                return false;
            }

            Log.Info("Deleted replication controller {ControllerName} for server {ServerId}.",
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

                ServiceV1 createdService = await _kubeClient.ServicesV1.Create(
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

                ServiceV1 createdService = await _kubeClient.ServicesV1.Create(
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

                StatusV1 result = await _kubeClient.ServicesV1.Delete(
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

                StatusV1 result = await _kubeClient.ServicesV1.Delete(
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
            int? externalPort = await _kubeClient.GetServerPublicPort(CurrentState);
            if (externalPort != null)
            {
                if (_clusterPublicDomainName != CurrentState.PublicFQDN || externalPort != CurrentState.PublicPort)
                {
                    Log.Info("Server {ServerName} is accessible at {ClusterPublicFQDN}:{PublicPortPort}",
                        CurrentState.Name,
                        _clusterPublicDomainName,
                        externalPort.Value
                    );

                    _dataAccess.Tell(
                        new ServerIngressChanged(_serverId, _clusterPublicDomainName, externalPort)
                    );

                    // Capture current ingress details to enable subsequent provisioning actions (if any).
                    CurrentState.PublicFQDN = _clusterPublicDomainName;
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
                    _dataAccess.Tell(
                        new ServerIngressChanged(_serverId, publicFQDN: null, publicPort: null)
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
            Log.Info("Initialising configuration for server {ServerId}...", _serverId);
            
            CommandResult commandResult = await _sqlClient.ExecuteCommand(
                serverId: CurrentState.Id,
                databaseId: SqlApiClient.MasterDatabaseId,
                sql: ManagementSql.ConfigureServerMemory(maxMemoryMB: 500 * 1024),
                executeAsAdminUser: true
            );

            for (int messageIndex = 0; messageIndex < commandResult.Messages.Count; messageIndex++)
            {
                Log.Info("T-SQL message [{MessageIndex}] from server {ServerId}: {TSqlMessage}",
                    messageIndex,
                    _serverId,
                    commandResult.Messages[messageIndex]
                );
            }

            if (!commandResult.Success)
            {
                foreach (SqlError error in commandResult.Errors)
                {
                    Log.Warning("Error encountered while initialising configuration for server {ServerId} ({ErrorKind}: {ErrorMessage})",
                        _serverId,
                        error.Kind,
                        error.Message
                    );
                }

                throw new SqlExecutionException($"One or more errors were encountered while configuring server (Id: {_serverId}).",
                    serverId: CurrentState.Id,
                    databaseId: SqlApiClient.MasterDatabaseId,
                    sqlMessages: commandResult.Messages,
                    sqlErrors: commandResult.Errors
                );
            }

            Log.Info("Configuration initialised for server {ServerId}.", _serverId);
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
            _dataAccess.Tell(
                new ServerProvisioning(_serverId, phase)
            );

            Log.Info("Starting provisioning phase {Phase} for server {ServerId}.", phase, _serverId);
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
            _dataAccess.Tell(
                new ServerReconfiguring(_serverId, phase)
            );

            Log.Info("Starting reconfiguration phase {Phase} for server {ServerId}.", phase, _serverId);
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
            _dataAccess.Tell(
                new ServerDeprovisioning(_serverId, phase)
            );

            Log.Info("Started de-provisioning phase {Phase} for server {ServerId}.", phase, _serverId);
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
                    _dataAccess.Tell(
                        new ServerProvisioningFailed(_serverId)
                    );

                    break;
                }
                case ProvisioningAction.Reconfigure:
                {
                    _dataAccess.Tell(
                        new ServerReconfigurationFailed(_serverId)
                    );

                    break;
                }
                case ProvisioningAction.Deprovision:
                {
                    _dataAccess.Tell(
                        new ServerDeprovisioningFailed(_serverId)
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
                    _dataAccess.Tell(
                        new ServerProvisioned(_serverId)
                    );

                    break;
                }
                case ProvisioningAction.Reconfigure:
                {
                    _dataAccess.Tell(
                        new ServerReconfigured(_serverId)
                    );

                    break;
                }
                case ProvisioningAction.Deprovision:
                {
                    _dataAccess.Tell(
                        new ServerDeprovisioned(_serverId)
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
        ///     Create a new <see cref="KubeApiClient"/> for communicating with the Kubernetes API.
        /// </summary>
        /// <returns>
        ///     The configured <see cref="KubeApiClient"/>.
        /// </returns>
        KubeApiClient CreateKubeApiClient()
        {
            if (Environment.GetEnvironmentVariable("IN_KUBERNETES") != "1")
            {
                return KubeApiClient.Create(
                    endPointUri: new Uri(
                        Context.System.Settings.Config.GetString("daas.kube.api-endpoint")
                    ),
                    accessToken: Context.System.Settings.Config.GetString("daas.kube.api-token")
                );
            }
            else
                return KubeApiClient.CreateFromPodServiceAccount();
        }

        /// <summary>
        ///     Create a new <see cref="SqlApiClient"/> for communicating with the SQL Executor API.
        /// </summary>
        /// <returns>
        ///     The configured <see cref="SqlApiClient"/>.
        /// </returns>
        SqlApiClient CreateSqlApiClient()
        {
            return SqlApiClient.Create(
                endPointUri: new Uri(
                    Context.System.Settings.Config.GetString("daas.sql.api-endpoint")
                )
            );
        }

        /// <summary>
        ///     Signal messages used to tell the <see cref="TenantServerManager"/> to perform an action.
        /// </summary>
        public enum Signal
        {
            /// <summary>
            ///     Poll the status of the server's ReplicationController.
            /// </summary>
            PollReplicationController = 1,

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
