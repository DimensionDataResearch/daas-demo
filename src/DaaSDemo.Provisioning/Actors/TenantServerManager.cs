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

    /// <summary>
    ///     Actor that represents a tenant's database server and manages its life-cycle.
    /// </summary>
    /// <remarks>
    ///     Management of the server's databases is delegated to a child <see cref="TenantDatabaseManager"/> actor.
    /// 
    ///     TODO: Connect to server as part of deployment and perform post-deploy configuration (e.g. limit memory usage).
    ///     TODO: Handle database status indicating an in-progress deployment.
    ///           This will allow us to pick up where we left off if we crash while deploying.
    ///     TODO: Create a Job after deploying the ReplicationController and Service that runs SQLCMD to configure the database server.
    ///           Mount the script into the job's pod as a Secret volume.
    /// </remarks>
    public class TenantServerManager
        : ReceiveActorEx, IWithUnboundedStash
    {
        /// <summary>
        ///     The pseudo-identifier referring to the "master" database on the tenant server.
        /// </summary>
        const int MasterDatabaseId = 0;

        /// <summary>
        ///     References to <see cref="TenantDatabaseManager"/> actors, keyed by database Id.
        /// </summary>
        readonly Dictionary<int, IActorRef> _databaseManagers = new Dictionary<int, IActorRef>();

        /// <summary>
        ///     State for <see cref="SqlRunner"/> actors, keyed by database Id.
        /// </summary>
        readonly Dictionary<int, SqlRunnerState> _sqlRunners = new Dictionary<int, SqlRunnerState>();

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
        ///     External IP addresses for Kubernetes nodes, keyed by the node's internal IP.
        /// </summary>
        ImmutableDictionary<string, string> _nodeExternalIPs = ImmutableDictionary<string, string>.Empty;

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

            Become(Ready);
        }

        /// <summary>
        ///     The actor's local message-stash facility.
        /// </summary>
        public IStash Stash { get; set; }

        /// <summary>
        ///     Current state (if known) from the database.
        /// </summary>
        DatabaseServer Currentstate { get; set; }

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

            base.PostStop();
        }

        /// <summary>
        ///     Called when the actor is ready to handle requests.
        /// </summary>
        void Ready()
        {
            Log.Info("Ready to process requests for server {ServerId} to complete.", _serverId);

            ReceiveAsync<DatabaseServer>(async databaseServer =>
            {
                Log.Info("Received server configuration (Id:{ServerId}, Name:{ServerName}).",
                    databaseServer.Id,
                    databaseServer.Name
                );

                PreviousState = Currentstate;
                Currentstate = databaseServer;

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
        ///     Called when the actor is waiting for initialisation of the server's configuration to complete.
        /// </summary>
        void InitializingServerConfiguration()
        {
            Log.Info("Waiting for initialisation of configuration for server {ServerId} to complete.", _serverId);

            ReceiveAsync<SqlExecuted>(async sqlExecuted =>
            {
                if (sqlExecuted.DatabaseName != "master")
                {
                    Log.Error("Received T-SQL execution result for unexpected database named {DatabaseName} in server {ServerId} (expected database {MasterDatabaseName}, since the server is currently being provisioned).",
                        sqlExecuted.DatabaseName,
                        _serverId,
                        "master"
                    );

                    _dataAccess.Tell(
                        new ServerProvisioningFailed(_serverId)
                    );
                    
                    Become(Ready);

                    return;
                }

                SqlRunnerState runnerState;
                if (_sqlRunners.TryGetValue(MasterDatabaseId, out runnerState))
                {
                    runnerState.IsBusy = false;

                    if (sqlExecuted.Success)
                    {
                        Log.Info("Configuration initialised for server {ServerId}.", _serverId);

                        await UpdateServerState(); // Pick up where we left off.
                    }
                    else
                        Log.Warning("Failed to initialise configuration server {ServerId} ({Reason})", _serverId, sqlExecuted.Result);
                }
                else
                {
                    Log.Error("Received T-SQL execution result for {DatabaseName} database in server {ServerId}, but no SqlRunner for this database was found.",
                        sqlExecuted.DatabaseName,
                        _serverId
                    );

                    _dataAccess.Tell(
                        new ServerProvisioningFailed(_serverId)
                    );
                }

                Become(Ready);
            });

            Receive<DatabaseServer>(_ =>
            {
                // Nothing we can do with it right now; we'll get another notification sooner or later.
                //
                // TODO: Consider whether this would be where we could allow for cancellation of the provisioning process.
                //
                Log.Info("Ignoring status notification for server {ServerId} (still waiting for initialisation of server configuration to complete).", _serverId);
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
        ///     Update the server state in Kubernetes.
        /// </summary>
        /// <returns>
        ///     A <see cref="Task"/> representing the operation.
        /// </returns>
        async Task UpdateServerState()
        {
            switch (Currentstate.Action)
            {
                case ProvisioningAction.Provision:
                {
                    Log.Info("Provisioning server {ServerId}...", Currentstate.Id);

                    try
                    {
                        await ProvisionServer();
                    }
                    catch (Exception provisioningFailed)
                    {
                        Log.Error(provisioningFailed, "Failed to provision server {ServerId}.",
                            KubeResources.GetBaseName(Currentstate),
                            Currentstate.Id
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
                    Log.Info("De-provisioning server {ServerId}...", Currentstate.Id);

                    try
                    {
                        await DeprovisionServer();
                    }
                    catch (Exception provisioningFailed)
                    {
                        Log.Error(provisioningFailed, "Failed to de-provision server {ServerId}.",
                            Currentstate.Id
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
                default:
                {
                    break;
                }
            }

            await UpdateServerIngressDetails();

            foreach (DatabaseInstance database in Currentstate.Databases)
            {
                Log.Info("Server configuration includes database {DatabaseName} (Id:{ServerId}).",
                    database.Name,
                    database.Id
                );

                IActorRef databaseManager;
                if (!_databaseManagers.TryGetValue(database.Id, out databaseManager))
                {
                    databaseManager = Context.ActorOf(
                        Props.Create(() => new TenantDatabaseManager(_dataAccess)),
                        name: TenantDatabaseManager.ActorName(database.Id)
                    );
                    Context.Watch(databaseManager);
                    _databaseManagers.Add(database.Id, databaseManager);

                    Log.Info("Created TenantDatabaseManager {ActorName} for server {ServerId} (Tenant:{TenantId}).",
                        databaseManager.Path.Name,
                        Currentstate.Id,
                        Currentstate.TenantId
                    );
                }

                // Hook up reverse-navigation property because TenantDatabaseManager will need server connection info.
                database.DatabaseServer = Currentstate;

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

            foreach ((int databaseId, SqlRunnerState runnerState) in _sqlRunners)
            {
                if (Equals(terminated.ActorRef, runnerState.Runner))
                {
                    Log.Info("SqlRunner for database {DatabaseId} in server {ServerId} has terminated.",
                        databaseId,
                        _serverId
                    );
                    
                    _sqlRunners.Remove(databaseId); // SQL runner terminated.

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
            switch (Currentstate.Phase)
            {
                case ServerProvisioningPhase.None:
                {
                    await EnsureReplicationControllerPresent();

                    goto case ServerProvisioningPhase.ReplicationController;
                }
                case ServerProvisioningPhase.ReplicationController:
                {
                    await EnsureServicePresent();

                    goto case ServerProvisioningPhase.Service;
                }
                case ServerProvisioningPhase.Service:
                {
                    InitialiseServerConfiguration();

                    Become(InitializingServerConfiguration);

                    return;
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
            switch (Currentstate.Phase)
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
                 labelSelector: $"cloud.dimensiondata.daas.server-id = {Currentstate.Id}"
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
                labelSelector: $"cloud.dimensiondata.daas.server-id = {Currentstate.Id}"
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
                labelSelector: $"cloud.dimensiondata.daas.server-id = {Currentstate.Id}"
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
                    Currentstate.Id
                );

                return existingController;
            }

            Log.Info("Creating replication controller for server {ServerId}...",
                Currentstate.Id
            );

            V1ReplicationController createdController = await _kubeClient.ReplicationControllersV1.Create(
                KubeResources.ReplicationController(Currentstate,
                    dataVolumeClaimName: Context.System.Settings.Config.GetString("daas.kube.volume-claim-name")
                )
            );

            Log.Info("Successfully created replication controller {ReplicationControllerName} for server {ServerId}.",
                createdController.Metadata.Name,
                Currentstate.Id
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
                Currentstate.Id
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
                    Currentstate.Id,
                    deleteFailed.Response.Message,
                    deleteFailed.Response.Reason
                );

                return false;
            }

            Log.Info("Deleted replication controller {ControllerName} for server {ServerId}.",
                controller.Metadata.Name,
                Currentstate.Id
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
                    Currentstate.Id
                );

                return existingService;
            }

            Log.Info("Creating service for server {ServerId}...",
                Currentstate.Id
            );

            V1Service createdService = await _kubeClient.ServicesV1.Create(
                KubeResources.Service(Currentstate)
            );

            Log.Info("Successfully created service {ServiceName} for server {ServerId}.",
                createdService.Metadata.Name,
                Currentstate.Id
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
                Currentstate.Id
            );

            UnversionedStatus result = await _kubeClient.ServicesV1.Delete(
                name: service.Metadata.Name
            );

            if (result.Status != "Success" && result.Reason != "NotFound")
            {
                Log.Error("Failed to delete service {ServiceName} for server {ServerId} (Message:{FailureMessage}, Reason:{FailureReason}).",
                    service.Metadata.Name,
                    Currentstate.Id,
                    result.Message,
                    result.Reason
                );

                return false;
            }

            Log.Info("Deleted service {ServiceName} for server {ServerId}.",
                service.Metadata.Name,
                Currentstate.Id
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
                    Currentstate.Id
                );

                return ingress;
            }

            Log.Info("Creating ingress for server {ServerId}...",
                Currentstate.Id
            );

            V1Beta1VoyagerIngress createdIngress = await _kubeClient.VoyagerIngressesV1Beta1.Create(
                KubeResources.Ingress(Currentstate)
            );

            Log.Info("Successfully created ingress {IngressName} for server {ServerId}.",
                createdIngress.Metadata.Name,
                Currentstate.Id
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
                Currentstate.Id
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
                        Currentstate.Id,
                        deleteFailed.Response.Message,
                        deleteFailed.Response.Reason
                    );

                    return false;
                }
            }

            Log.Info("Deleted ingress {IngressName} for server {ServerId}.",
                ingress.Metadata.Name,
                Currentstate.Id
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
                        Currentstate.Name,
                        ingressIP,
                        ingressPort.Value
                    );

                    if (ingressIP != Currentstate.IngressIP || ingressPort != Currentstate.IngressPort)
                    {
                        _dataAccess.Tell(
                            new ServerIngressChanged(_serverId, ingressIP, ingressPort)
                        );

                        // Capture current ingress details to enable subsequent provisioning actions.
                        Currentstate.IngressIP = ingressIP;
                        Currentstate.IngressPort = ingressPort;
                    }

                    if (Currentstate.Status == ProvisioningStatus.Provisioning)
                    {
                        _dataAccess.Tell(
                            new ServerProvisioned(_serverId)
                        );
                    }
                }
                else
                {
                    Log.Info("Cannot determine host port for server {ServerName}.", Currentstate.Name);

                    if (Currentstate.IngressIP != null)
                    {
                        _dataAccess.Tell(
                            new ServerIngressChanged(_serverId, Currentstate.IngressIP, ingressPort: null)
                        );
                    }
                }
            }
            else
            {
                Log.Info("Cannot determine host IP for server {ServerName}.", Currentstate.Name);

                if (Currentstate.IngressIP != null)
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
            (string hostIP, int? hostPort) = await _kubeClient.GetServerIngressEndPoint(Currentstate);
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
        void InitialiseServerConfiguration()
        {
            SetProvisioningPhase(ServerProvisioningPhase.InitializeConfiguration);

            Log.Info("Initialising configuration for server {ServerId}...",
                Currentstate.Id
            );
            
            SqlRunnerState runnerState;
            if (!_sqlRunners.TryGetValue(MasterDatabaseId, out runnerState))
            {
                IActorRef sqlRunner = Context.ActorOf(
                    Props.Create(
                        () => new SqlRunner(Self, Currentstate)
                    ),
                    name: $"sqlcmd-{_serverId}-master"
                );
                Context.Watch(sqlRunner);

                runnerState = new SqlRunnerState(sqlRunner);
                _sqlRunners.Add(MasterDatabaseId, runnerState);
            }

            runnerState.Runner.Tell(new ExecuteSql(
                databaseName: "master",
                jobNameSuffix: "initialize-configuration",
                sql: ManagementSql.ConfigureServerMemory(maxMemoryMB: 500 * 1024)
            ));
            runnerState.IsBusy = true;

            Log.Info("Waiting for initialisation of configuration server {ServerId} to complete...",
                Currentstate.Id
            );
        }

        /// <summary>
        ///     Set and persist the current provisioning phase.
        /// </summary>
        /// <param name="phase">
        ///     The current provisioning phase.
        /// </param>
        void SetProvisioningPhase(ServerProvisioningPhase phase)
        {
            Currentstate.Phase = phase;
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
            Currentstate.Phase = phase;
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
        ///     Get the name of the <see cref="TenantServerManager"/> actor for the specified tenant.
        /// </summary>
        /// <param name="tenantId">
        ///     The tenant Id.
        /// </param>
        /// <returns>
        ///     The actor name.
        /// </returns>
        public static string ActorName(int tenantId) => $"server-manager.{tenantId}";

        /// <summary>
        ///     Represents the state for an <see cref="SqlRunner"/> actor.
        /// </summary>
        class SqlRunnerState
        {
            /// <summary>
            ///     Create a new <see cref="SqlRunnerState"/>.
            /// </summary>
            /// <param name="runner">
            ///     A reference to the <see cref="SqlRunner"/> actor.
            /// </param>
            public SqlRunnerState(IActorRef runner)
            {
                if (runner == null)
                    throw new ArgumentNullException(nameof(runner));
                
                Runner = runner;
            }

            /// <summary>
            ///     A reference to the <see cref="SqlRunner"/> actor.
            /// </summary>
            public IActorRef Runner { get; }

            /// <summary>
            ///     Is the runner currently busy?
            /// </summary>
            public bool IsBusy { get; set; }
        }
    }
}
