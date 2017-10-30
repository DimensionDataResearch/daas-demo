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
    using Data;
    using Data.Models;
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
        : ReceiveActorEx
    {
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
        ///     The <see cref="HttpClient"/> used to communicate with the Kubernetes API.
        /// </summary>
        readonly HttpClient _client;

        /// <summary>
        ///     Previous state (if known) from the database.
        /// </summary>
        DatabaseServer _previousState;

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

            _client = CreateKubeClient();

            ReceiveAsync<DatabaseServer>(UpdateServerState);
            Receive<IPAddressMappingsChanged>(mappingsChanged =>
            {
                _nodeExternalIPs = mappingsChanged.Mappings;

                // TODO: Work out how / when to invalidate ingress IP (if required).
            });
            Receive<Terminated>(terminated =>
            {
                int? databaseId =
                    _databaseManagers.Where(
                        entry => Equals(entry.Value, terminated.ActorRef)
                    )
                    .Select(
                        entry => (int?)entry.Key
                    )
                    .FirstOrDefault();

                if (databaseId.HasValue)
                    _databaseManagers.Remove(databaseId.Value); // Database manager terminated.
                else
                    Unhandled(terminated);
            });
        }

        /// <summary>
        ///     Called when the actor has stopped.
        /// </summary>
        protected override void PostStop()
        {
            _client.CancelPendingRequests();
            _client.Dispose();

            base.PostStop();
        }

        /// <summary>
        ///     Update the server state in Kubernetes.
        /// </summary>
        /// <param name="server">
        ///     A <see cref="DatabaseServer"/> representing the target server state.
        /// </param>
        /// <returns>
        ///     A <see cref="Task"/> representing the operation.
        /// </returns>
        async Task UpdateServerState(DatabaseServer server)
        {
            if (server == null)
                throw new ArgumentNullException(nameof(server));

            Log.Info("Received server configuration (Id:{ServerId}, Name:{ServerName}).",
                server.Id,
                server.Name
            );

            switch (server.Action)
            {
                case ProvisioningAction.Provision:
                {
                    Log.Info("Provisioning server {ServerId}...", server.Id);

                    _dataAccess.Tell(
                        new ServerProvisioning(_serverId)
                    );

                    try
                    {
                        await DeployServer(server);
                    }
                    catch (Exception provisioningFailed)
                    {
                        Log.Error(provisioningFailed, "Failed to provision server {ServerId}.",
                            GetBaseResourceName(server),
                            server.Id
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
                    Log.Info("De-provisioning server {ServerId}...", server.Id);

                    _dataAccess.Tell(
                        new ServerDeprovisioning(_serverId)
                    );

                    try
                    {
                        await DestroyServer(server);
                    }
                    catch (Exception provisioningFailed)
                    {
                        Log.Error(provisioningFailed, "Failed to de-provision server {ServerId}.",
                            GetBaseResourceName(server),
                            server.Id
                        );

                        _dataAccess.Tell(
                            new ServerDeprovisioningFailed(_serverId)
                        );

                        return;
                    }

                    _dataAccess.Tell(
                        new ServerDeprovisioned(_serverId)
                    );

                    Context.Stop(Self);

                    return;
                }
                default:
                {
                    break;
                }
            }

            string ingressIP = await GetIngressHostIP(server);
            if (!String.IsNullOrWhiteSpace(ingressIP))
            {
                int? ingressPort = await GetIngressHostPort(server);
                if (ingressPort != null)
                {
                    Log.Info("Server {ServerName} is accessible at {HostIP}:{HostPort}",
                        server.Name,
                        ingressIP,
                        ingressPort.Value
                    );

                    if (ingressIP != server.IngressIP || ingressPort != server.IngressPort)
                    {
                        _dataAccess.Tell(
                            new ServerIngressChanged(_serverId, ingressIP, ingressPort)
                        );

                        // Capture current ingress details to enable subsequent provisioning actions.
                        server.IngressIP = ingressIP;
                        server.IngressPort = ingressPort;
                    }

                    if (server.Status == ProvisioningStatus.Provisioning)
                    {
                        // TODO: Connect to the server and perform initial configuration (e.g. max memory usage).
                        // TODO: Alternatively, consider using a Job and Secret to just run SQLCMD inside the cluster with a generated provisioning script (so we don't need the ingress).

                        _dataAccess.Tell(
                            new ServerProvisioned(_serverId)
                        );
                    }
                }
                else
                {
                    Log.Info("Cannot determine host port for server {ServerName}.", server.Name);

                    if (server.IngressIP != null)
                    {
                        _dataAccess.Tell(
                            new ServerIngressChanged(_serverId, server.IngressIP, ingressPort: null)
                        );
                    }
                }
            }
            else
            {
                Log.Info("Cannot determine host IP for server {ServerName}.", server.Name);

                if (server.IngressIP != null)
                {
                    _dataAccess.Tell(
                        new ServerIngressChanged(_serverId, ingressIP: null, ingressPort: null)
                    );
                }
            }

            foreach (DatabaseInstance database in server.Databases)
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
                    _databaseManagers.Add(database.Id, databaseManager);

                    Log.Info("Created TenantDatabaseManager {ActorName} for server {ServerId} (Tenant:{TenantId}).",
                        databaseManager.Path.Name,
                        server.Id,
                        server.TenantId
                    );
                }

                // Hook up reverse-navigation property because TenantDatabaseManager will need server connection info.
                database.DatabaseServer = server;

                databaseManager.Tell(database);
            }

            _previousState = server;
        }

        /// <summary>
        ///     Deploy an instance of SQL Server.
        /// </summary>
        /// <param name="server">
        ///     A <see cref="DatabaseServer"/> describing the server.
        /// </param>
        /// <returns>
        ///     A <see cref="Task"/> representing the operation.
        /// </returns>
        async Task DeployServer(DatabaseServer server)
        {
            if (server == null)
                throw new ArgumentNullException(nameof(server));

            await EnsureReplicationControllerPresent(server);
            await EnsureServicePresent(server);
            await EnsureIngressPresent(server);
        }

        /// <summary>
        ///     Destroy an instance of SQL Server.
        /// </summary>
        /// <param name="server">
        ///     A <see cref="DatabaseServer"/> describing the server.
        /// </param>
        /// <returns>
        ///     A <see cref="Task"/> representing the operation.
        /// </returns>
        async Task DestroyServer(DatabaseServer server)
        {
            if (server == null)
                throw new ArgumentNullException(nameof(server));

            await EnsureIngressAbsent(server);
            await EnsureServiceAbsent(server);
            await EnsureReplicationControllerAbsent(server);
        }

        /// <summary>
        ///     Find the server's associated ReplicationController (if it exists).
        /// </summary>
        /// <param name="server">
        ///     A <see cref="DatabaseServer"/> representing the target server.
        /// </param>
        /// <returns>
        ///     The ReplicationController, or <c>null</c> if it was not found.
        /// </returns>
        async Task<V1ReplicationController> FindReplicationController(DatabaseServer server)
        {
            if (server == null)
                throw new ArgumentNullException(nameof(server));

            V1ReplicationControllerList matchingControllers =
                await _client.GetAsync(
                    HttpRequest.Factory.Json("api/v1/namespaces/default/replicationcontrollers")
                        .WithQueryParameter("labelSelector", $"cloud.dimensiondata.daas.server-id = {server.Id}")
                )
                .ReadContentAsAsync<V1ReplicationControllerList, UnversionedStatus>();

            if (matchingControllers.Items.Count == 0)
                return null;

            return matchingControllers.Items[matchingControllers.Items.Count - 1];
        }

        /// <summary>
        ///     Find the server's associated Service (if it exists).
        /// </summary>
        /// <param name="server">
        ///     A <see cref="DatabaseServer"/> representing the target server.
        /// </param>
        /// <returns>
        ///     The Service, or <c>null</c> if it was not found.
        /// </returns>
        async Task<V1Service> FindService(DatabaseServer server)
        {
            if (server == null)
                throw new ArgumentNullException(nameof(server));

            V1ServiceList matchingServices =
                await _client.GetAsync(
                    HttpRequest.Factory.Json("api/v1/namespaces/default/services")
                        .WithQueryParameter("labelSelector", $"cloud.dimensiondata.daas.server-id = {server.Id}")
                )
                .ReadContentAsAsync<V1ServiceList, UnversionedStatus>();

            if (matchingServices.Items.Count == 0)
                return null;

            return matchingServices.Items[matchingServices.Items.Count - 1];
        }

        /// <summary>
        ///     Determine whether the server's associated Ingress exists.
        /// </summary>
        /// <param name="server">
        ///     A <see cref="DatabaseServer"/> representing the target server.
        /// </param>
        /// <returns>
        ///     <c>true</c>, if the Ingress exists; otherwise, <c>false</c>.
        /// </returns>
        async Task<V1Beta1VoyagerIngress> FindIngress(DatabaseServer server)
        {
            if (server == null)
                throw new ArgumentNullException(nameof(server));

            string baseName = GetBaseResourceName(server);

            HttpResponseMessage response = await _client.GetAsync(
                HttpRequest.Factory.Json("apis/voyager.appscode.com/v1beta1/namespaces/default/ingresses/{IngressName}")
                    .WithTemplateParameter("IngressName",
                        value: $"{baseName}-ingress"
                    )
            );
            using (response)
            {
                switch (response.StatusCode)
                {
                    case HttpStatusCode.OK:
                    {
                        return await response.ReadContentAsAsync<V1Beta1VoyagerIngress>();
                    }
                    case HttpStatusCode.NotFound:
                    {
                        return null;
                    }
                    default:
                    {
                        UnversionedStatus status = await response.ReadContentAsAsync<UnversionedStatus>();

                        throw new HttpRequestException<UnversionedStatus>(response.StatusCode, status);
                    }
                }
            }
        }

        /// <summary>
        ///     Ensure that a ReplicationController resource exists for the specified database server.
        /// </summary>
        /// <param name="server">
        ///     A <see cref="DatabaseServer"/> representing the target server.
        /// </param>
        /// <returns>
        ///     The ReplicationController resource, as a <see cref="V1ReplicationController"/>.
        /// </returns>
        async Task<V1ReplicationController> EnsureReplicationControllerPresent(DatabaseServer server)
        {
            if (server == null)
                throw new ArgumentNullException(nameof(server));

            V1ReplicationController existingController = await FindReplicationController(server);
            if (existingController != null)
            {
                Log.Info("Found existing replication controller {ReplicationControllerName} for server {ServerId}.",
                    existingController.Metadata.Name,
                    server.Id
                );

                return existingController;
            }

            Log.Info("Creating replication controller for server {ServerId}...",
                server.Id
            );

            string baseName = GetBaseResourceName(server);

            V1ReplicationController newController = new V1ReplicationController
            {
                ApiVersion = "v1",
                Kind = "ReplicationController",
                Metadata = new V1ObjectMeta
                {
                    Name = baseName
                },
                Spec = new V1ReplicationControllerSpec
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
                                        ClaimName = Context.System.Settings.Config.GetString("daas.kube.volume-claim-name") // TODO: Make this dynamically-configurable.
                                    }
                                }
                            }
                        }
                    }
                }
            };

            V1ReplicationController createdController =
                await _client.PostAsJsonAsync(
                    request: HttpRequest.Factory.Json("api/v1/namespaces/default/replicationcontrollers"),
                    postBody: newController
                )
                .ReadContentAsAsync<V1ReplicationController, UnversionedStatus>();

            Log.Info("Successfully created replication controller {ReplicationControllerName} for server {ServerId}.",
                createdController.Metadata.Name,
                server.Id
            );

            return createdController;
        }

        /// <summary>
        ///     Ensure that a ReplicationController resource does not exist for the specified database server.
        /// </summary>
        /// <param name="server">
        ///     A <see cref="DatabaseServer"/> representing the target server.
        /// </param>
        /// <returns>
        ///     <c>true</c>, if the controller is now absent; otherwise, <c>false</c>.
        /// </returns>
        async Task<bool> EnsureReplicationControllerAbsent(DatabaseServer server)
        {
            if (server == null)
                throw new ArgumentNullException(nameof(server));
            
            V1ReplicationController controller = await FindReplicationController(server);
            if (controller == null)
                return true;

            Log.Info("Deleting replication controller {ControllerName} for server {ServerId}...",
                controller.Metadata.Name,
                server.Id
            );

            try
            {
                await _client.DeleteAsJsonAsync(
                    HttpRequest.Factory.Json("api/v1/namespaces/default/replicationcontrollers/{ControllerName}")
                        .WithTemplateParameter("ControllerName", controller.Metadata.Name),
                    deleteBody: new
                    {
                        apiVersion = "v1",
                        kind = "DeleteOptions",
                        propagationPolicy = "Background"
                    }
                )
                .ReadContentAsAsync<UnversionedStatus, UnversionedStatus>();
            }
            catch (HttpRequestException<UnversionedStatus> deleteFailed)
            {
                if (deleteFailed.Response.Reason != "NotFound")
                {
                    Log.Error("Failed to delete replication controller {ControllerName} for server {ServerId} (Message:{FailureMessage}, Reason:{FailureReason}).",
                        controller.Metadata.Name,
                        server.Id,
                        deleteFailed.Response.Message,
                        deleteFailed.Response.Reason
                    );

                    return false;
                }
            }

            Log.Info("Deleted replication controller {ControllerName} for server {ServerId}.",
                controller.Metadata.Name,
                server.Id
            );

            return true;
        }

        /// <summary>
        ///     Ensure that a Service resource exists for the specified database server.
        /// </summary>
        /// <param name="server">
        ///     A <see cref="DatabaseServer"/> representing the target server.
        /// </param>
        /// <returns>
        ///     The Service resource, as a <see cref="V1Service"/>.
        /// </returns>
        async Task<V1Service> EnsureServicePresent(DatabaseServer server)
        {
            if (server == null)
                throw new ArgumentNullException(nameof(server));

            V1Service existingService = await FindService(server);
            if (existingService != null)
            {
                Log.Info("Found existing service {ServiceName} for server {ServerId}.",
                    existingService.Metadata.Name,
                    server.Id
                );

                return existingService;
            }

            Log.Info("Creating service for server {ServerId}...",
                server.Id
            );

            string baseName = GetBaseResourceName(server);

            V1Service newService = new V1Service
            {
                ApiVersion = "v1",
                Kind = "Service",
                Metadata = new V1ObjectMeta
                {
                    Name = $"{baseName}-service",
                    Labels = new Dictionary<string, string>
                    {
                        ["k8s-app"] = baseName,
                        ["cloud.dimensiondata.daas.server-id"] = server.Id.ToString()
                    }
                },
                Spec = new V1ServiceSpec
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
                }
            };

            V1Service createdService =
                await _client.PostAsJsonAsync(
                    request: HttpRequest.Factory.Json("api/v1/namespaces/default/services"),
                    postBody: newService
                )
                .ReadContentAsAsync<V1Service, UnversionedStatus>();

            Log.Info("Successfully created service {ServiceName} for server {ServerId}.",
                createdService.Metadata.Name,
                server.Id
            );

            return createdService;
        }

        /// <summary>
        ///     Ensure that a Service resource does not exist for the specified database server.
        /// </summary>
        /// <param name="server">
        ///     A <see cref="DatabaseServer"/> representing the target server.
        /// </param>
        /// <returns>
        ///     <c>true</c>, if the service is now absent; otherwise, <c>false</c>.
        /// </returns>
        async Task<bool> EnsureServiceAbsent(DatabaseServer server)
        {
            if (server == null)
                throw new ArgumentNullException(nameof(server));
            
            V1Service service = await FindService(server);
            if (service == null)
                return true;

            Log.Info("Deleting service {ServiceName} for server {ServerId}...",
                service.Metadata.Name,
                server.Id
            );

            UnversionedStatus result =
                await _client.DeleteAsync(
                    HttpRequest.Factory.Json("api/v1/namespaces/default/services/{ServiceName}")
                        .WithTemplateParameter("ServiceName",
                            value: service.Metadata.Name
                        )
                )
                .ReadContentAsAsync<UnversionedStatus>(HttpStatusCode.OK, HttpStatusCode.NotFound);

            if (result.Status != "Success" && result.Reason != "NotFound")
            {
                Log.Error("Failed to delete service {ServiceName} for server {ServerId} (Message:{FailureMessage}, Reason:{FailureReason}).",
                    service.Metadata.Name,
                    server.Id,
                    result.Message,
                    result.Reason
                );

                return false;
            }

            Log.Info("Deleted service {ServiceName} for server {ServerId}.",
                service.Metadata.Name,
                server.Id
            );

            return true;
        }

        /// <summary>
        ///     Ensure that an Ingress resource exists for the specified database server.
        /// </summary>
        /// <param name="server">
        ///     A <see cref="DatabaseServer"/> representing the target server.
        /// </param>
        /// <returns>
        ///     The Ingress resource, as a <see cref="V1Beta1VoyagerIngress"/>.
        /// </returns>
        async Task<V1Beta1VoyagerIngress> EnsureIngressPresent(DatabaseServer server)
        {
            if (server == null)
                throw new ArgumentNullException(nameof(server));

            V1Beta1VoyagerIngress ingress = await FindIngress(server);
            if (ingress != null)
            {
                Log.Info("Found existing ingress {IngressName} for server {ServerId}.",
                    ingress.Metadata.Name,
                    server.Id
                );

                return ingress;
            }

            Log.Info("Creating ingress for server {ServerId}...",
                server.Id
            );

            string baseName = GetBaseResourceName(server);

            var newIngress = new V1Beta1VoyagerIngress
            {
                ApiVersion = "voyager.appscode.com/v1beta1",
                Kind = "Ingress",
                Metadata = new V1ObjectMeta
                {
                    Name = $"{baseName}-ingress",
                    Labels = new Dictionary<string, string>
                    {
                        ["cloud.dimensiondata.daas.server-id"] = server.Id.ToString()
                    },
                    Annotations = new Dictionary<string, string>
                    {
                        ["ingress.appscode.com/type"] = "HostPort",
                        ["kubernetes.io/ingress.class"] = "voyager"
                    }
                },
                Spec = new V1BetaVoyagerIngressSpec
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
                }
            };

            V1Beta1VoyagerIngress createdIngress =
                await _client.PostAsJsonAsync(
                    request: HttpRequest.Factory.Json("apis/voyager.appscode.com/v1beta1/namespaces/default/ingresses"),
                    postBody: newIngress
                )
                .ReadContentAsAsync<V1Beta1VoyagerIngress, UnversionedStatus>();

            Log.Info("Successfully created ingress {IngressName} for server {ServerId}.",
                createdIngress.Metadata.Name,
                server.Id
            );

            return createdIngress;
        }

        /// <summary>
        ///     Ensure that an Ingress resource does not exist for the specified database server.
        /// </summary>
        /// <param name="server">
        ///     A <see cref="DatabaseServer"/> representing the target server.
        /// </param>
        /// <returns>
        ///     <c>true</c>, if the ingress is now absent; otherwise, <c>false</c>.
        /// </returns>
        async Task<bool> EnsureIngressAbsent(DatabaseServer server)
        {
            if (server == null)
                throw new ArgumentNullException(nameof(server));

            V1Beta1VoyagerIngress ingress = await FindIngress(server);
            if (ingress == null)
                return true;

            Log.Info("Deleting ingress {IngressName} for server {ServerId}...",
                ingress.Metadata.Name,
                server.Id
            );

            try
            {
                await _client.DeleteAsync(
                    HttpRequest.Factory.Json("apis/voyager.appscode.com/v1beta1/namespaces/default/ingresses/{IngressName}")
                        .WithTemplateParameter("IngressName",
                            value: ingress.Metadata.Name
                        )
                )
                .ReadContentAsAsync<V1Beta1VoyagerIngress, UnversionedStatus>();
            }
            catch (HttpRequestException<UnversionedStatus> deleteFailed)
            {
                if (deleteFailed.Response.Reason != "NotFound")
                {
                    Log.Error("Failed to delete replication service {ServiceName} for server {ServerId} (Message:{FailureMessage}, Reason:{FailureReason}).",
                        ingress.Metadata.Name,
                        server.Id,
                        deleteFailed.Response.Message,
                        deleteFailed.Response.Reason
                    );

                    return false;
                }
            }

            Log.Info("Deleted ingress {IngressName} for server {ServerId}.",
                ingress.Metadata.Name,
                server.Id
            );

            return true;
        }

        /// <summary>
        ///     Get the (external) IP on which the database server is accessible.
        /// </summary>
        /// <param name="server">
        ///     A <see cref="DatabaseServer"/> describing the server.
        /// </param>
        /// <returns>
        ///     The IP, or <c>null</c> if the ingress for the server cannot be found.
        /// </returns>
        async Task<string> GetIngressHostIP(DatabaseServer server)
        {
            if (server == null)
                throw new ArgumentNullException(nameof(server));

            string ingressResourceName = $"sql-server-{server.Id}-ingress";

            // Find the Pod that implements the ingress (the origin-name label will point back to the ingress resource).
            V1PodList matchingPods =
                await _client.GetAsync(
                    HttpRequest.Factory.Json("api/v1/namespaces/default/pods")
                        .WithQueryParameter("labelSelector", $"origin-name = {ingressResourceName}")
                )
                .ReadContentAsAsync<V1PodList, UnversionedStatus>();

            V1Pod ingressPod = matchingPods.Items.FirstOrDefault(item => item.Status.Phase == "Running");
            if (ingressPod == null)
                return null;

            if (_nodeExternalIPs.TryGetValue(ingressPod.Status.HostIP, out string hostExternalIP))
                return hostExternalIP;

            return ingressPod.Status.HostIP;
        }

        /// <summary>
        ///     Get the (external) port on which the database server is accessible.
        /// </summary>
        /// <param name="server">
        ///     A <see cref="DatabaseServer"/> describing the server.
        /// </param>
        /// <returns>
        ///     The port, or <c>null</c> if the ingress for the server cannot be found.
        /// </returns>
        async Task<int?> GetIngressHostPort(DatabaseServer server)
        {
            if (server == null)
                throw new ArgumentNullException(nameof(server));

            string entityName = $"sql-server-{server.Id}-ingress";

            V1PodList matchingPods =
                await _client.GetAsync(
                    HttpRequest.Factory.Json("api/v1/namespaces/default/pods")
                        .WithQueryParameter("labelSelector", $"origin-name = {entityName}")
                )
                .ReadContentAsAsync<V1PodList, UnversionedStatus>();

            V1Pod ingressPod = matchingPods.Items.FirstOrDefault(item => item.Status.Phase == "Running");
            if (ingressPod == null)
                return null;

            return ingressPod.Spec.Containers[0].Ports[0].HostPort;
        }

        /// <summary>
        ///     Create a new <see cref="HttpClient"/> for communicating with the Kubernetes API.
        /// </summary>
        /// <returns>
        ///     The configured <see cref="HttpClient"/>.
        /// </returns>
        HttpClient CreateKubeClient()
        {
            return new HttpClient
            {
                BaseAddress = new Uri(
                    Context.System.Settings.Config.GetString("daas.kube.api-endpoint")
                ),
                DefaultRequestHeaders =
                {
                    Authorization = new AuthenticationHeaderValue(
                        scheme: "Bearer",
                        parameter: Context.System.Settings.Config.GetString("daas.kube.api-token")
                    )
                }
            };
        }

        /// <summary>
        ///     Get the base resource name for the specified server.
        /// </summary>
        /// <param name="server">
        ///     A <see cref="DatabaseServer"/> representing the target server
        /// </param>
        /// <returns>
        ///     The base resource name.
        /// </returns>
        static string GetBaseResourceName(DatabaseServer server) => $"sql-server-{server.Id}";

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

    /// <summary>
    ///     A <see cref="V1VolumeMount"/> with the "subPath" property.
    /// </summary>
    [DataContract]
    class V1VolumeMountWithSubPath
        : V1VolumeMount
    {
        /// <summary>
        ///     The volume sub-path (if any).
        /// </summary>
        [DataMember(Name = "subPath", EmitDefaultValue = false)]
        public string SubPath { get; set; }
    }

    [DataContract]
    class V1Beta1VoyagerIngress
    {
        [DataMember(Name = "apiVersion", EmitDefaultValue = false)]
        public string ApiVersion { get; set; }

        [DataMember(Name = "kind", EmitDefaultValue = false)]
        public string Kind { get; set; }

        [DataMember(Name = "metadata", EmitDefaultValue = false)]
        public V1ObjectMeta Metadata { get; set; }

        [DataMember(Name = "spec", EmitDefaultValue = false)]
        public V1BetaVoyagerIngressSpec Spec { get; set; }

        public V1beta1IngressStatus Status { get; set; }
    }

    [DataContract]
    class V1BetaVoyagerIngressSpec
    {
        [DataMember(Name = "tls", EmitDefaultValue = false)]
        public List<V1beta1IngressTLS> Tls { get; set; }

        [DataMember(Name = "rules", EmitDefaultValue = false)]
        public List<V1Beta1VoyagerIngressRule> Rules { get; set; }
    }

    [DataContract]
    class V1Beta1VoyagerIngressRule
    {
        [DataMember(Name = "host", EmitDefaultValue = false)]
        public string Host { get; set; }

        [DataMember(Name = "tcp", EmitDefaultValue = false)]
        public V1Beta1VoyagerIngressRuleTcp Tcp { get; set; }
    }

    [DataContract]
    class V1Beta1VoyagerIngressRuleTcp
    {
        [DataMember(Name = "backend", EmitDefaultValue = false)]
        public V1beta1IngressBackend Backend { get; set; }

        [DataMember(Name = "port", EmitDefaultValue = false)]
        public string Port { get; set; }
    }
}
