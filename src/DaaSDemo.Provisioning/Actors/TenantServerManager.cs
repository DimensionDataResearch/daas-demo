using Akka;
using Akka.Actor;
using Akka.DI.Core;
using HTTPlease;
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
    using Common.Options;
    using Common.Utilities;
    using Data;
    using KubeClient.Models;
    using Messages;
    using Models.Data;
    using Provisioners;

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
        readonly Dictionary<string, IActorRef> _databaseManagers = new Dictionary<string, IActorRef>();

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
        /// <param name="provisioner">
        ///     Provisioning facility for the target server.
        /// </param>
        /// <param name="kubeResources">
        ///     The Kubernetes resource factory.
        /// </param>
        /// <param name="kubeClient">
        ///     The <see cref="KubeApiClient"/> used to communicate with the Kubernetes API.
        /// </param>
        /// <param name="kubeOptions">
        ///     Application-level Kubernetes settings.
        /// </param>
        /// <param name="sqlClient">
        ///     The <see cref="DatabaseProxyApiClient"/> used to communicate with the Database Proxy API.
        /// </param>
        public TenantServerManager(DatabaseServerProvisioner provisioner, ServerCredentialsProvisioner credentialsProvisioner, IOptions<KubernetesOptions> kubeOptions)
        {
            if (provisioner == null)
                throw new ArgumentNullException(nameof(provisioner));

            if (credentialsProvisioner == null)
                throw new ArgumentNullException(nameof(credentialsProvisioner));

            if (kubeOptions == null)
                throw new ArgumentNullException(nameof(kubeOptions));

            Provisioner = provisioner;
            CredentialsProvisioner = credentialsProvisioner;
            KubeOptions = kubeOptions.Value;
        }

        /// <summary>
        ///     The actor's local message-stash facility.
        /// </summary>
        public IStash Stash { get; set; }

        /// <summary>
        ///     Provisioning facility for the target server.
        /// </summary>
        DatabaseServerProvisioner Provisioner { get; }

        /// <summary>
        ///     Credential-provisioning facility for the target server.
        /// </summary>
        ServerCredentialsProvisioner CredentialsProvisioner { get; }

        /// <summary>
        ///     Application-level Kubernetes settings.
        /// </summary>
        KubernetesOptions KubeOptions { get; set; }

        /// <summary>
        ///     A reference to the <see cref="Actors.DataAccess"/> actor.
        /// </summary>
        IActorRef DataAccess { get; set; }

        /// <summary>
        ///     The Id of the target server.
        /// </summary>
        string ServerId { get; set; }

        /// <summary>
        ///     Called when the actor is started.
        /// </summary>
        protected override void PreStart() => Become(Initializing);

        /// <summary>
        ///     Called when the actor is waiting for its <see cref="Initialize"/> message.
        /// </summary>
        void Initializing()
        {
            Receive<Initialize>(initialize =>
            {
                DataAccess = initialize.DataAccess;
                ServerId = initialize.ServerId;
                Provisioner.State = initialize.InitialState.Clone();
                CredentialsProvisioner.State = initialize.InitialState.Clone();

                Self.Tell(Provisioner.State); // Kick off initial state-management actions.

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

                Provisioner.State = databaseServer.Clone();
                CredentialsProvisioner.State = databaseServer.Clone();

                await UpdateServerState();
            });
            Receive<DatabaseInstance>(database =>
            {
                if (Provisioner.State.Status == ProvisioningStatus.Ready)
                {
                    Log.Debug("Received database configuration (Id:{DatabaseId}, Name:{DatabaseName}).",
                        database.Id,
                        database.Name
                    );

                    UpdateDatabaseState(database);
                }
                else
                {
                    Log.Debug("Ignoring database configuration (Id:{DatabaseId}, Name:{DatabaseName}) because server {ServerId} is not ready.",
                        database.Id,
                        database.Name,
                        ServerId
                    );
                }
                
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
                switch (Provisioner.State.Action)
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
                        if (Provisioner.State.Status == ProvisioningStatus.Ready)
                        {
                            Become(Ready);

                            break;
                        }

                        DeploymentV1Beta1 deployment = await Provisioner.FindDeployment();
                        if (deployment == null)
                        {
                            Log.Warning("{Action} failed - cannot find Deployment for server {ServerId}.", actionDescription, ServerId);

                            FailCurrentAction(
                                reason: $"Cannot find server's associated Deployment in Kubernetes."
                            );

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
                            if (Provisioner.State.Action == ProvisioningAction.Provision)
                                StartProvisioningPhase(ServerProvisioningPhase.Configuration);
                            else if (Provisioner.State.Action == ProvisioningAction.Reconfigure)
                                StartReconfigurationPhase(ServerProvisioningPhase.Configuration);
                            else
                            {
                                Log.Error("WaitForServerAvailable: Unexpected provisioning action '{Action}' on server {ServerId}.",
                                    Provisioner.State.Action,
                                    Provisioner.State.Id
                                );

                                FailCurrentAction(
                                    reason: $"Server has unexpected provisioning action ({Provisioner.State.Action})."
                                );

                                return;
                            }

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

                        FailCurrentAction(
                            reason: "Timed out waiting for server's associated Deployment in Kubernetes to become available."
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
            switch (Provisioner.State.Action)
            {
                case ProvisioningAction.Provision:
                {
                    Log.Info("Provisioning server {ServerId} ({Phase})...", Provisioner.State.Id, Provisioner.State.Phase);

                    try
                    {
                        await ProvisionServer();
                    }
                    catch (HttpRequestException<StatusV1> provisioningFailed)
                    {
                        Log.Error(provisioningFailed, "Failed to provision server {ServerId} ({Reason}): {ErrorMessage}",
                            Provisioner.State.Id,
                            provisioningFailed.Response.Reason,
                            provisioningFailed.Response.Message
                        );

                        FailCurrentAction(
                            reason: $"Failed to provision server {Provisioner.State.Id} ({provisioningFailed.Response.Reason}): {provisioningFailed.Response.Message}"
                        );

                        return;
                    }
                    catch (Exception provisioningFailed)
                    {
                        Log.Error(provisioningFailed, "Failed to provision server {ServerId}.",
                            Provisioner.State.Id
                        );

                        FailCurrentAction(
                            reason: $"Failed to provision server (unexpected error): " + provisioningFailed.Message
                        );

                        return;
                    }

                    break;
                }
                case ProvisioningAction.Reconfigure:
                {
                    Log.Info("Reconfiguring server {ServerId} ({Phase})...", Provisioner.State.Id, Provisioner.State.Phase);

                    try
                    {
                        await ReconfigureServer();
                    }
                    catch (HttpRequestException<StatusV1> reconfigurationFailed)
                    {
                        Log.Error(reconfigurationFailed, "Failed to reconfigure server {ServerId} ({Reason}): {ErrorMessage}",
                            Provisioner.State.Id,
                            reconfigurationFailed.Response.Reason,
                            reconfigurationFailed.Response.Message
                        );

                        FailCurrentAction(
                            reason: $"Failed to reconfigure server {Provisioner.State.Id} ({reconfigurationFailed.Response.Reason}): {reconfigurationFailed.Response.Message}"
                        );

                        return;
                    }
                    catch (Exception reconfigurationFailed)
                    {
                        Log.Error(reconfigurationFailed, "Failed to reconfigure server {ServerId}.",
                            Provisioner.State.Id
                        );

                        FailCurrentAction(
                            reason: $"Failed to reconfigure server (unexpected error): " + reconfigurationFailed.Message
                        );

                        return;
                    }

                    break;
                }
                case ProvisioningAction.Deprovision:
                {
                    Log.Info("De-provisioning server {ServerId} ({Phase})...", Provisioner.State.Id, Provisioner.State.Phase);

                    try
                    {
                        await DeprovisionServer();
                    }
                    catch (HttpRequestException<StatusV1> deprovisioningFailed)
                    {
                        Log.Error(deprovisioningFailed, "Failed to de-provision server {ServerId} ({Reason}): {ErrorMessage}",
                            Provisioner.State.Id,
                            deprovisioningFailed.Response.Reason,
                            deprovisioningFailed.Response.Message
                        );

                        FailCurrentAction(
                            reason: $"Failed to de-provision server {Provisioner.State.Id} ({deprovisioningFailed.Response.Reason}): {deprovisioningFailed.Response.Message}"
                        );

                        return;
                    }
                    catch (Exception deprovisioningFailed)
                    {
                        Log.Error(deprovisioningFailed, "Failed to de-provision server {ServerId}.",
                            Provisioner.State.Id
                        );

                        FailCurrentAction(
                            reason: $"Failed to de-provision server (unexpected error): " + deprovisioningFailed.Message
                        );

                        return;
                    }

                    // Like tears in rain, time to die.
                    Context.Stop(Self);

                    return;
                }
            }

            await UpdateServerIngressDetails();
        }

        /// <summary>
        ///     Update the database to converge with desired state.
        /// </summary>
        /// <param name="database">
        ///     A <see cref="DatabaseInstance"/> representing the desired state.
        /// </param>
        void UpdateDatabaseState(DatabaseInstance database)
        {
            IActorRef databaseManager;
            if (!_databaseManagers.TryGetValue(database.Id, out databaseManager))
            {
                databaseManager = Context.ActorOf(
                    Context.DI().Props<TenantDatabaseManager>()
                        .WithSupervisorStrategy(StandardSupervision.Default),
                    name: TenantDatabaseManager.ActorName(database.Id)
                );
                Context.Watch(databaseManager);
                _databaseManagers.Add(database.Id, databaseManager);

                databaseManager.Tell(new TenantDatabaseManager.Initialize(
                    serverManager: Self,
                    dataAccess: DataAccess,
                    server: Provisioner.State.Clone(),
                    initialState: database
                ));

                Log.Info("Created TenantDatabaseManager {ActorName} for database {DatabaseId} in server {ServerId} (Tenant:{TenantId}).",
                    databaseManager.Path.Name,
                    database.Id,
                    Provisioner.State.Id,
                    Provisioner.State.TenantId
                );
            }
            else
            {
                Log.Debug("Notifying TenantDatabaseManager {ActorName} of current configuration for database {DatabaseId}.", databaseManager.Path.Name, database.Id);
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
            
            foreach ((string databaseId, IActorRef databaseManager) in _databaseManagers)
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
            switch (Provisioner.State.Phase)
            {
                case ServerProvisioningPhase.None:
                {
                    goto case ServerProvisioningPhase.Storage;
                }
                case ServerProvisioningPhase.Storage:
                {
                    StartProvisioningPhase(ServerProvisioningPhase.Storage);

                    await Provisioner.EnsureDataVolumeClaimPresent();
                    
                    goto case ServerProvisioningPhase.Security;
                }
                case ServerProvisioningPhase.Security:
                {
                    StartProvisioningPhase(ServerProvisioningPhase.Security);

                    await CredentialsProvisioner.EnsureCredentialsSecretPresent();
                    
                    goto case ServerProvisioningPhase.Instance;
                }
                case ServerProvisioningPhase.Instance:
                {
                    StartProvisioningPhase(ServerProvisioningPhase.Instance);

                    await Provisioner.EnsureDeploymentPresent();
                    
                    goto case ServerProvisioningPhase.Network;
                }
                case ServerProvisioningPhase.Network:
                {
                    StartProvisioningPhase(ServerProvisioningPhase.Network);

                    await Provisioner.EnsureInternalServicePresent();

                    goto case ServerProvisioningPhase.Monitoring;
                }
                case ServerProvisioningPhase.Monitoring:
                {
                    StartProvisioningPhase(ServerProvisioningPhase.Monitoring);

                    await Provisioner.EnsureServiceMonitorPresent();

                    Become(WaitForServerAvailable); // We can't proceed until the deployment becomes available.

                    break;
                }
                case ServerProvisioningPhase.Configuration:
                {
                    StartProvisioningPhase(ServerProvisioningPhase.Configuration);

                    await Provisioner.InitialiseServerConfiguration();

                    goto case ServerProvisioningPhase.Ingress;
                }
                case ServerProvisioningPhase.Ingress:
                {
                    StartProvisioningPhase(ServerProvisioningPhase.Ingress);

                    await Provisioner.EnsureExternalServicePresent();

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
            switch (Provisioner.State.Phase)
            {
                case ServerProvisioningPhase.None:
                {
                    goto case ServerProvisioningPhase.Storage;
                }
                case ServerProvisioningPhase.Storage:
                {
                    StartReconfigurationPhase(ServerProvisioningPhase.Storage);

                    await Provisioner.EnsureDataVolumeClaimPresent();
                    
                    goto case ServerProvisioningPhase.Security;
                }
                case ServerProvisioningPhase.Security:
                {
                    StartReconfigurationPhase(ServerProvisioningPhase.Security);

                    await CredentialsProvisioner.EnsureCredentialsSecretPresent();
                    
                    goto case ServerProvisioningPhase.Instance;
                }
                case ServerProvisioningPhase.Instance:
                {
                    StartReconfigurationPhase(ServerProvisioningPhase.Instance);

                    await Provisioner.EnsureDeploymentPresent();
                    
                    goto case ServerProvisioningPhase.Network;
                }
                case ServerProvisioningPhase.Network:
                {
                    StartReconfigurationPhase(ServerProvisioningPhase.Network);

                    await Provisioner.EnsureInternalServicePresent();

                    goto case ServerProvisioningPhase.Monitoring;
                }
                case ServerProvisioningPhase.Monitoring:
                {
                    StartReconfigurationPhase(ServerProvisioningPhase.Monitoring);

                    await Provisioner.EnsureServiceMonitorPresent();

                    Become(WaitForServerAvailable); // We can't proceed until the deployment becomes available.

                    break;
                }
                case ServerProvisioningPhase.Configuration:
                {
                    StartReconfigurationPhase(ServerProvisioningPhase.Configuration);
                    
                    await Provisioner.InitialiseServerConfiguration();

                    goto case ServerProvisioningPhase.Ingress;
                }
                case ServerProvisioningPhase.Ingress:
                {
                    StartReconfigurationPhase(ServerProvisioningPhase.Ingress);

                    await Provisioner.EnsureExternalServicePresent();

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
            switch (Provisioner.State.Phase)
            {
                case ServerProvisioningPhase.None:
                {
                    goto case ServerProvisioningPhase.Ingress;
                }
                case ServerProvisioningPhase.Ingress:
                {
                    StartDeprovisioningPhase(ServerProvisioningPhase.Ingress);

                    await Provisioner.EnsureExternalServiceAbsent();

                    goto case ServerProvisioningPhase.Monitoring;
                }
                case ServerProvisioningPhase.Monitoring:
                {
                    StartDeprovisioningPhase(ServerProvisioningPhase.Monitoring);
                    
                    await Provisioner.EnsureServiceMonitorAbsent();

                    goto case ServerProvisioningPhase.Network;
                }
                case ServerProvisioningPhase.Network:
                {
                    StartDeprovisioningPhase(ServerProvisioningPhase.Network);
                    
                    await Provisioner.EnsureInternalServiceAbsent();

                    goto case ServerProvisioningPhase.Instance;
                }
                case ServerProvisioningPhase.Instance:
                {
                    StartDeprovisioningPhase(ServerProvisioningPhase.Instance);

                    await Provisioner.EnsureDeploymentAbsent();
                    
                    goto case ServerProvisioningPhase.Security;
                }
                case ServerProvisioningPhase.Security:
                {
                    StartDeprovisioningPhase(ServerProvisioningPhase.Security);

                    await CredentialsProvisioner.EnsureCredentialsSecretAbsent();
                    
                    goto case ServerProvisioningPhase.Storage;
                }
                case ServerProvisioningPhase.Storage:
                {
                    StartDeprovisioningPhase(ServerProvisioningPhase.Storage);

                    await Provisioner.EnsureDataVolumeClaimAbsent();
                    
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
        ///     Update ingress details for the database server.
        /// </summary>
        /// <returns>
        ///     A <see cref="Task"/> representing the operation.
        /// </returns>
        async Task UpdateServerIngressDetails()
        {
            int? externalPort = await Provisioner.GetPublicPort();
            if (externalPort != null)
            {
                string serverFQDN = $"{Provisioner.State.Name}.database.{KubeOptions.ClusterPublicFQDN}";
                if (serverFQDN != Provisioner.State.PublicFQDN || externalPort != Provisioner.State.PublicPort)
                {
                    Log.Info("Server {ServerName} is accessible at {ClusterPublicFQDN}:{PublicPortPort}",
                        Provisioner.State.Name,
                        serverFQDN,
                        externalPort.Value
                    );

                    DataAccess.Tell(
                        new ServerIngressChanged(ServerId, serverFQDN, externalPort)
                    );

                    // Capture current ingress details to enable subsequent provisioning actions (if any).
                    Provisioner.State.PublicFQDN = serverFQDN;
                    Provisioner.State.PublicPort = externalPort;
                }

                if (Provisioner.State.Phase == ServerProvisioningPhase.Ingress)
                    CompleteCurrentAction();
            }
            else
            {
                Log.Debug("Cannot determine public port for server {ServerName}.", Provisioner.State.Name);

                if (Provisioner.State.PublicFQDN != null)
                {
                    DataAccess.Tell(
                        new ServerIngressChanged(ServerId, publicFQDN: null, publicPort: null)
                    );
                }
            }
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
            Provisioner.State.Phase = phase;
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
            Provisioner.State.Phase = phase;
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
            Provisioner.State.Phase = phase;
            DataAccess.Tell(
                new ServerDeprovisioning(ServerId, phase)
            );

            Log.Info("Started de-provisioning phase {Phase} for server {ServerId}.", phase, ServerId);
        }

        /// <summary>
        ///     Fail the current provision / reconfigure / de-provision action.
        /// </summary>
        /// <param name="reason">
        ///     A message indicating the reason for the failure.
        /// </param>
        void FailCurrentAction(string reason)
        {
            Provisioner.State.Status = ProvisioningStatus.Error;
            switch (Provisioner.State.Action)
            {
                case ProvisioningAction.Provision:
                {
                    DataAccess.Tell(
                        new ServerProvisioningFailed(ServerId, reason)
                    );

                    break;
                }
                case ProvisioningAction.Reconfigure:
                {
                    DataAccess.Tell(
                        new ServerReconfigurationFailed(ServerId, reason)
                    );

                    break;
                }
                case ProvisioningAction.Deprovision:
                {
                    DataAccess.Tell(
                        new ServerDeprovisioningFailed(ServerId, reason)
                    );

                    break;
                }
            }

            Provisioner.State.Phase = ServerProvisioningPhase.None;
        }

        /// <summary>
        ///     Complete the current provision / reconfigure / de-provision action.
        /// </summary>
        void CompleteCurrentAction()
        {
            switch (Provisioner.State.Action)
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
                Provisioner.State.Action,
                Provisioner.State.Id
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
            public string ServerId => InitialState.Id;

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
        public static string ActorName(string tenantId) => $"server-manager.{tenantId}";
    }
}
