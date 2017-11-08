using Akka;
using Akka.Actor;
using HTTPlease;
using KubeNET.Swagger.Model;
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
    using Data.Models;
    using KubeClient;
    using KubeClient.Models;
    using Messages;
    using Models.Sql;
    using SqlExecutor.Client;

    /// <summary>
    ///     Actor that represents a tenant's database server and manages its life-cycle.
    /// </summary>
    /// <remarks>
    ///     Management of the server's databases is delegated to a child <see cref="TenantDatabaseManager"/> actor.
    /// 
    ///     TODO: Implement IsServerInstanceReady (check ReplicationController.AvailableReplicas)
    ///           and poll for state until AvailableReplicas is equal to Replicas before proceeding.
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
        ///     External IP addresses for Kubernetes nodes, keyed by the node's internal IP.
        /// </summary>
        ImmutableDictionary<string, string> _nodeExternalIPs = ImmutableDictionary<string, string>.Empty;

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

            Become(Ready);
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
                Log.Info("Received server configuration (Id:{ServerId}, Name:{ServerName}).",
                    databaseServer.Id,
                    databaseServer.Name
                );

                PreviousState = CurrentState;
                CurrentState = databaseServer;

                await UpdateServerState();
            });
            Receive<IPAddressMappingsChanged>(mappingsChanged =>
            {
                _nodeExternalIPs = mappingsChanged.Mappings;
            });
            Receive<Terminated>(
                terminated => HandleTermination(terminated)
            );
        }

        /// <summary>
        ///     Called when the actor is waiting for the server's ReplicationController to indicate that all replicas are Ready.
        /// </summary>
        void WaitForServerAvailable()
        {
            Log.Info("Waiting for server {ServerId}'s ReplicationController to become Ready...", _serverId);

            StartPolling(Signal.PollReplicationController);

            ReceiveAsync<Signal>(async signal =>
            {
                switch (signal)
                {
                    case Signal.PollReplicationController:
                    {
                        // TODO: Check if replication controller is available yet.
                        V1ReplicationController replicationController = await FindReplicationController();
                        if (replicationController == null)
                        {
                            Log.Warning("Provisioning failed - cannot find ReplicationController for server {ServerId}.", _serverId);

                            _dataAccess.Tell(
                                new ServerProvisioningFailed(_serverId)
                            );

                            Become(Ready);
                        }
                        else if (replicationController.Status.AvailableReplicas == replicationController.Status.Replicas)
                        {
                            SetProvisioningPhase(ServerProvisioningPhase.Service);

                            Become(Ready);
                        }
                        else
                        {
                            Log.Info("Server {ServerID} is not available yet ({AvailableReplicaCount} of {ReplicaCount} replicas are marked as ready).",
                                _serverId,
                                replicationController.Status.AvailableReplicas ?? 0,
                                replicationController.Status.Replicas
                            );
                        }

                        break;
                    }
                    case Signal.Timeout:
                    {
                        Log.Warning("Provisioning failed - timed out waiting server {ServerId}'s ReplicationController to become ready.", _serverId);

                        _dataAccess.Tell(
                            new ServerProvisioningFailed(_serverId)
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
            });
            Receive<DatabaseServer>(_ =>
            {
                Log.Debug("Ignoring DatabaseServer state message (waiting for server's ReplicationController to be ready).'");
            });
            Receive<IPAddressMappingsChanged>(mappingsChanged =>
            {
                _nodeExternalIPs = mappingsChanged.Mappings;
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
                    Log.Info("Provisioning server {ServerId}...", CurrentState.Id);

                    try
                    {
                        await ProvisionServer();
                    }
                    catch (Exception provisioningFailed)
                    {
                        Log.Error(provisioningFailed, "Failed to provision server {ServerId}.",
                            CurrentState.Id
                        );

                        _dataAccess.Tell(
                            new ServerProvisioningFailed(_serverId)
                        );

                        return;
                    }

                    break;
                }
                case ProvisioningAction.Deprovision:
                {
                    Log.Info("De-provisioning server {ServerId}...", CurrentState.Id);

                    try
                    {
                        await DeprovisionServer();
                    }
                    catch (Exception provisioningFailed)
                    {
                        Log.Error(provisioningFailed, "Failed to de-provision server {ServerId}.",
                            CurrentState.Id
                        );

                        _dataAccess.Tell(
                            new ServerDeprovisioningFailed(_serverId)
                        );

                        return;
                    }

                    _dataAccess.Tell(
                        new ServerDeprovisioned(_serverId)
                    );

                    // Like tears in rain, time to die.
                    Context.Stop(Self);

                    return;
                }
            }

            await UpdateServerIngressDetails();
            
            UpdateDatabaseState();
        }

        /// <summary>
        ///     Update the server's databases to converge with desired state.
        /// </summary>
        void UpdateDatabaseState()
        {
            foreach (DatabaseInstance database in CurrentState.Databases)
            {
                Log.Info("Server configuration includes database {DatabaseName} (Id:{ServerId}).",
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

                    Log.Info("Created TenantDatabaseManager {ActorName} for server {ServerId} (Tenant:{TenantId}).",
                        databaseManager.Path.Name,
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
                    await EnsureReplicationControllerPresent();

                    goto case ServerProvisioningPhase.ReplicationController;
                }
                case ServerProvisioningPhase.ReplicationController:
                {
                    await EnsureServicePresent();

                    Become(WaitForServerAvailable); // We can't proceed until the replication controller becomes available.

                    break;
                }
                case ServerProvisioningPhase.Service:
                {
                    await InitialiseServerConfiguration();

                    goto case ServerProvisioningPhase.InitializeConfiguration;
                }
                case ServerProvisioningPhase.InitializeConfiguration:
                {
                    await EnsureIngressPresent();

                    goto case ServerProvisioningPhase.Ingress;
                }
                case ServerProvisioningPhase.Ingress:
                {
                    SetProvisioningPhase(ServerProvisioningPhase.None);

                    break;
                }
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
                    await EnsureReplicationControllerAbsent();

                    goto case ServerProvisioningPhase.ReplicationController;
                }
                case ServerProvisioningPhase.ReplicationController:
                {
                    await EnsureServiceAbsent();

                    goto case ServerProvisioningPhase.Service;
                }
                case ServerProvisioningPhase.Service:
                {
                    await EnsureIngressAbsent();

                    goto case ServerProvisioningPhase.Ingress;
                }
                case ServerProvisioningPhase.Ingress:
                {
                    SetProvisioningPhase(ServerProvisioningPhase.None);

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
        async Task<V1ReplicationController> FindReplicationController()
        {
            List<V1ReplicationController> matchingControllers = await _kubeClient.ReplicationControllersV1.List(
                 labelSelector: $"cloud.dimensiondata.daas.server-id = {CurrentState.Id}"
             );

            if (matchingControllers.Count == 0)
                return null;

            return matchingControllers[matchingControllers.Count - 1];
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
                labelSelector: $"cloud.dimensiondata.daas.server-id = {CurrentState.Id}"
            );

            if (matchingServices.Count == 0)
                return null;

            return matchingServices[matchingServices.Count - 1];
        }

        /// <summary>
        ///     Determine whether the server's associated Ingress exists.
        /// </summary>
        /// <returns>
        ///     <c>true</c>, if the Ingress exists; otherwise, <c>false</c>.
        /// </returns>
        async Task<V1Beta1VoyagerIngress> FindIngress()
        {
            List<V1Beta1VoyagerIngress> matchingIngresses = await _kubeClient.VoyagerIngressesV1Beta1.List(
                labelSelector: $"cloud.dimensiondata.daas.server-id = {CurrentState.Id}"
            );

            if (matchingIngresses.Count == 0)
                return null;

            return matchingIngresses[matchingIngresses.Count - 1];
        }

        /// <summary>
        ///     Ensure that a ReplicationController resource exists for the specified database server.
        /// </summary>
        /// <returns>
        ///     The ReplicationController resource, as a <see cref="V1ReplicationController"/>.
        /// </returns>
        async Task<V1ReplicationController> EnsureReplicationControllerPresent()
        {
            SetProvisioningPhase(ServerProvisioningPhase.ReplicationController);

            V1ReplicationController existingController = await FindReplicationController();
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

            V1ReplicationController createdController = await _kubeClient.ReplicationControllersV1.Create(
                KubeResources.ReplicationController(CurrentState,
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
            SetDeprovisioningPhase(ServerProvisioningPhase.ReplicationController);

            V1ReplicationController controller = await FindReplicationController();
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
            catch (HttpRequestException<UnversionedStatus> deleteFailed)
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
        ///     Ensure that a Service resource exists for the specified database server.
        /// </summary>
        /// <returns>
        ///     The Service resource, as a <see cref="V1Service"/>.
        /// </returns>
        async Task<V1Service> EnsureServicePresent()
        {
            SetProvisioningPhase(ServerProvisioningPhase.Service);

            V1Service existingService = await FindService();
            if (existingService != null)
            {
                Log.Info("Found existing service {ServiceName} for server {ServerId}.",
                    existingService.Metadata.Name,
                    CurrentState.Id
                );

                return existingService;
            }

            Log.Info("Creating service for server {ServerId}...",
                CurrentState.Id
            );

            V1Service createdService = await _kubeClient.ServicesV1.Create(
                KubeResources.Service(CurrentState)
            );

            Log.Info("Successfully created service {ServiceName} for server {ServerId}.",
                createdService.Metadata.Name,
                CurrentState.Id
            );

            return createdService;
        }

        /// <summary>
        ///     Ensure that a Service resource does not exist for the specified database server.
        /// </summary>
        /// <returns>
        ///     <c>true</c>, if the service is now absent; otherwise, <c>false</c>.
        /// </returns>
        async Task<bool> EnsureServiceAbsent()
        {
            SetDeprovisioningPhase(ServerProvisioningPhase.Service);

            V1Service service = await FindService();
            if (service == null)
                return true;

            Log.Info("Deleting service {ServiceName} for server {ServerId}...",
                service.Metadata.Name,
                CurrentState.Id
            );

            UnversionedStatus result = await _kubeClient.ServicesV1.Delete(
                name: service.Metadata.Name
            );

            if (result.Status != "Success" && result.Reason != "NotFound")
            {
                Log.Error("Failed to delete service {ServiceName} for server {ServerId} (Message:{FailureMessage}, Reason:{FailureReason}).",
                    service.Metadata.Name,
                    CurrentState.Id,
                    result.Message,
                    result.Reason
                );

                return false;
            }

            Log.Info("Deleted service {ServiceName} for server {ServerId}.",
                service.Metadata.Name,
                CurrentState.Id
            );

            return true;
        }

        /// <summary>
        ///     Ensure that an Ingress resource exists for the specified database server.
        /// </summary>
        /// <returns>
        ///     The Ingress resource, as a <see cref="V1Beta1VoyagerIngress"/>.
        /// </returns>
        async Task<V1Beta1VoyagerIngress> EnsureIngressPresent()
        {
            SetProvisioningPhase(ServerProvisioningPhase.Ingress);

            V1Beta1VoyagerIngress ingress = await FindIngress();
            if (ingress != null)
            {
                Log.Info("Found existing ingress {IngressName} for server {ServerId}.",
                    ingress.Metadata.Name,
                    CurrentState.Id
                );

                return ingress;
            }

            Log.Info("Creating ingress for server {ServerId}...",
                CurrentState.Id
            );

            V1Beta1VoyagerIngress createdIngress = await _kubeClient.VoyagerIngressesV1Beta1.Create(
                KubeResources.Ingress(CurrentState)
            );

            Log.Info("Successfully created ingress {IngressName} for server {ServerId}.",
                createdIngress.Metadata.Name,
                CurrentState.Id
            );

            return createdIngress;
        }

        /// <summary>
        ///     Ensure that an Ingress resource does not exist for the specified database server.
        /// </summary>
        /// <returns>
        ///     <c>true</c>, if the ingress is now absent; otherwise, <c>false</c>.
        /// </returns>
        async Task<bool> EnsureIngressAbsent()
        {
            SetDeprovisioningPhase(ServerProvisioningPhase.Ingress);

            V1Beta1VoyagerIngress ingress = await FindIngress();
            if (ingress == null)
                return true;

            Log.Info("Deleting ingress {IngressName} for server {ServerId}...",
                ingress.Metadata.Name,
                CurrentState.Id
            );

            try
            {
                await _kubeClient.VoyagerIngressesV1Beta1.Delete(
                    name: ingress.Metadata.Name
                );
            }
            catch (HttpRequestException<UnversionedStatus> deleteFailed)
            {
                if (deleteFailed.Response.Reason != "NotFound")
                {
                    Log.Error("Failed to delete replication service {ServiceName} for server {ServerId} (Message:{FailureMessage}, Reason:{FailureReason}).",
                        ingress.Metadata.Name,
                        CurrentState.Id,
                        deleteFailed.Response.Message,
                        deleteFailed.Response.Reason
                    );

                    return false;
                }
            }

            Log.Info("Deleted ingress {IngressName} for server {ServerId}.",
                ingress.Metadata.Name,
                CurrentState.Id
            );

            return true;
        }

        /// <summary>
        ///     Update ingress details for the database server.
        /// </summary>
        /// <returns>
        ///     A <see cref="Task"/> representing the operation.
        /// </returns>
        async Task UpdateServerIngressDetails()
        {
            (string ingressIP, int? ingressPort) = await GetServerIngress();
            if (!String.IsNullOrWhiteSpace(ingressIP))
            {
                if (ingressPort != null)
                {
                    Log.Info("Server {ServerName} is accessible at {HostIP}:{HostPort}",
                        CurrentState.Name,
                        ingressIP,
                        ingressPort.Value
                    );

                    if (ingressIP != CurrentState.IngressIP || ingressPort != CurrentState.IngressPort)
                    {
                        _dataAccess.Tell(
                            new ServerIngressChanged(_serverId, ingressIP, ingressPort)
                        );

                        // Capture current ingress details to enable subsequent provisioning actions.
                        CurrentState.IngressIP = ingressIP;
                        CurrentState.IngressPort = ingressPort;
                    }

                    if (CurrentState.Status == ProvisioningStatus.Provisioning)
                    {
                        _dataAccess.Tell(
                            new ServerProvisioned(_serverId)
                        );
                    }
                }
                else
                {
                    Log.Info("Cannot determine host port for server {ServerName}.", CurrentState.Name);

                    if (CurrentState.IngressIP != null)
                    {
                        _dataAccess.Tell(
                            new ServerIngressChanged(_serverId, CurrentState.IngressIP, ingressPort: null)
                        );
                    }
                }
            }
            else
            {
                Log.Info("Cannot determine host IP for server {ServerName}.", CurrentState.Name);

                if (CurrentState.IngressIP != null)
                {
                    _dataAccess.Tell(
                        new ServerIngressChanged(_serverId, ingressIP: null, ingressPort: null)
                    );
                }
            }
        }

        /// <summary>
        ///     Get the (external) IP and port on which the database server is accessible.
        /// </summary>
        /// <returns>
        ///     The IP and port, or <c>null</c> and <c>null</c> if the ingress for the server cannot be found.
        /// </returns>
        async Task<(string hostIP, int? hostPort)> GetServerIngress()
        {
            (string hostIP, int? hostPort) = await _kubeClient.GetServerIngressEndPoint(CurrentState);
            if (String.IsNullOrWhiteSpace(hostIP))
                return (hostIP: null, hostPort: null);

            string hostExternalIP;
            if (!_nodeExternalIPs.TryGetValue(hostIP, out hostExternalIP))
                return (hostIP: null, hostPort: null); // If we can't map to the corresponding external IP address, don't bother.

            return (hostExternalIP, hostPort);
        }

        /// <summary>
        ///     Execute T-SQL to initialise the server configuration.
        /// </summary>
        /// <returns>
        ///     A <see cref="Task"/> representing the operation.
        /// </returns>
        async Task InitialiseServerConfiguration()
        {
            SetProvisioningPhase(ServerProvisioningPhase.InitializeConfiguration);

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
                
                // TODO: Custom exception type.
                throw new Exception($"One or more errors were encountered while configuring server (Id: {_serverId}).");
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
        void SetProvisioningPhase(ServerProvisioningPhase phase)
        {
            CurrentState.Phase = phase;
            _dataAccess.Tell(
                new ServerProvisioning(_serverId, phase)
            );
        }

        /// <summary>
        ///     Set and persist the current de-provisioning phase.
        /// </summary>
        /// <param name="phase">
        ///     The current de-provisioning phase.
        /// </param>
        void SetDeprovisioningPhase(ServerProvisioningPhase phase)
        {
            CurrentState.Phase = phase;
            _dataAccess.Tell(
                new ServerDeprovisioning(_serverId, phase)
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
            return KubeApiClient.Create(
                endPointUri: new Uri(
                    Context.System.Settings.Config.GetString("daas.kube.api-endpoint")
                ),
                accessToken: Context.System.Settings.Config.GetString("daas.kube.api-token")
            );
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
