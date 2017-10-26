using Akka;
using Akka.Actor;
using HTTPlease;
using KubeNET.Swagger.Model;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
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
    ///     TODO: Store server's current deployment phase in the master database (but do not expose it to API clients).
    ///           This will allow us to pick up where we left off if we crash while deploying.
    /// </remarks>
    public class TenantServerManager
        : ReceiveActorEx
    {
        /// <summary>
        ///     External IP addresses for Kubernetes nodes, keyed by the node's internal IP.
        /// </summary>
        /// <remarks>
        ///     AF: Hard-coded for demo (still need to modify the DataAccess actor to pass these to us from the IPAddressMapping table).
        /// </remarks>
        readonly Dictionary<string, string> _nodeExternalIPs = new Dictionary<string, string>
        {
            ["192.168.5.20"] = "168.128.36.207",
            ["192.168.5.21"] = "168.128.36.94",
            ["192.168.5.22"] = " 168.128.36.206"
        };

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

            V1ReplicationController existingController = await FindReplicationController(server);
            if (existingController == null)
            {
                Log.Info("Replication controller for server {ServerId} does not exist; deploying...", server.Id);

                _dataAccess.Tell(
                    new ServerProvisioning(_serverId)
                );

                try
                {
                    await DeployServer(server);
                }
                catch (Exception provisioningFailed)
                {
                    Log.Error(provisioningFailed, "Failed to deploy server {ServerId}.",
                        GetBaseResourceName(server),
                        server.Id
                    );

                    _dataAccess.Tell(
                        new ServerProvisioningFailed(_serverId)
                    );

                    return;
                }
            }
            else
            {
                Log.Info("Found replication controller {ReplicationControllerName} for server {ServerId}.",
                    existingController.Metadata.Name,
                    server.Id
                );
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

                    if (server.Status != ProvisioningStatus.Ready)
                    {
                        _dataAccess.Tell(
                            new ServerProvisioned(_serverId)
                        );
                    }
                    else if (ingressIP != server.IngressIP || ingressPort != server.IngressPort)
                    {
                        _dataAccess.Tell(
                            new ServerIngressChanged(_serverId, ingressIP, ingressPort)
                        );
                    }
                }
                else
                {
                    Log.Info("Cannot determine host port for server {ServerName}.", server.Name);
                }
            }
            else
                Log.Info("Cannot determine host IP for server {ServerName}.", server.Name);

            foreach (DatabaseInstance database in server.Databases)
            {
                Log.Info("Server configuration includes database {DatabaseName} (Id:{ServerId}).",
                    database.Name,
                    database.Id
                );
            }

            _previousState = server;
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
                    HttpRequest.Factory.Json("/api/v1/namespaces/default/replicationcontrollers")
                        .WithQueryParameter("labelSelector", $"cloud.dimensiondata.daas.server-id = {server.Id}")
                )
                .ReadAsAsync<V1ReplicationControllerList>();

            if (matchingControllers.Items.Count == 0)
                return null;

            return matchingControllers.Items[matchingControllers.Items.Count - 1];
        }

        /// <summary>
        ///     Deploy an instance of SQL Server.
        /// </summary>
        /// <param name="server">
        ///     A <see cref="DatabaseServer"/> describing the server.
        /// </param>
        /// <returns>
        ///     A <see cref="V1ReplicationController"/> representing the Kubernetes ReplicationController for the server instance.
        /// </returns>
        async Task<V1ReplicationController> DeployServer(DatabaseServer server)
        {
            if (server == null)
                throw new ArgumentNullException(nameof(server));

            string baseName = GetBaseResourceName(server);

            // ReplicationController
            
            Log.Info("Creating replication controller for server {ServerId}...",
                server.Id
            );
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
                                            Value = server.AdminPassword
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
                    request: HttpRequest.Factory.Json("/api/v1/namespaces/default/replicationcontrollers"),
                    postBody: newController
                )
                .ReadAsAsync<V1ReplicationController>();

            Log.Info("Successfully created replication controller {ReplicationControllerName} for server {ServerId}.",
                createdController.Metadata.Name,
                server.Id
            );

            // Service

            Log.Info("Creating service for server {ServerId}...",
                server.Id
            );
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
                    request: HttpRequest.Factory.Json("/api/v1/namespaces/default/services"),
                    postBody: newService
                )
                .ReadAsAsync<V1Service>();

            Log.Info("Successfully created service {ServiceName} for server {ServerId}.",
                createdService.Metadata.Name,
                server.Id
            );

            // Ingress

            Log.Info("Creating ingress for server {ServerId}...",
                server.Id
            );
            JObject newIngress = JObject.FromObject(new
            {
                apiVersion = "voyager.appscode.com/v1beta1",
                kind = "Ingress",
                metadata = new
                {
                    name = $"{baseName}-ingress",
                    labels = new Dictionary<string, string>
                    {
                        ["cloud.dimensiondata.daas.server-id"] = server.Id.ToString()
                    },
                    annotations = new Dictionary<string, string>
                    {
                        ["ingress.appscode.com/type"] = "HostPort",
                        ["kubernetes.io/ingress.class"] = "voyager"
                    }
                },
                spec = new
                {
                    rules = new[]
                    {
                        new
                        {
                            host = $"{server.Name}.local",
                            tcp = new
                            {
                                port = (11433 + server.Id).ToString(), // Cheaty!
                                backend = new
                                {
                                    serviceName = $"{baseName}-service",
                                    servicePort = "1433"
                                }
                            }
                        }
                    }
                }
            });

            JObject createdIngress =
                await _client.PostAsJsonAsync(
                    request: HttpRequest.Factory.Json("apis/voyager.appscode.com/v1beta1/namespaces/default/ingresses"),
                    postBody: newIngress
                )
                .ReadAsAsync<JObject>();
            
            string ingressName = createdIngress.SelectToken("metadata.name")?.Value<string>();
            Log.Info("Successfully created ingress {IngressName} for server {ServerId}.",
                ingressName,
                server.Id
            );

            return createdController;
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

            string entityName = $"sql-server-{server.Id}-ingress";

            V1PodList matchingPods =
                await _client.GetAsync(
                    HttpRequest.Factory.Json("/api/v1/namespaces/default/pods")
                        .WithQueryParameter("labelSelector", $"origin-name = {entityName}")
                )
                .ReadAsAsync<V1PodList>();

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
                    HttpRequest.Factory.Json("/api/v1/namespaces/default/pods")
                        .WithQueryParameter("labelSelector", $"origin-name = {entityName}")
                )
                .ReadAsAsync<V1PodList>();

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
    }

    /// <summary>
    ///     A <see cref="V1VolumeMount"/> with the "subPath" property.
    /// </summary>
    class V1VolumeMountWithSubPath
        : V1VolumeMount
    {
        /// <summary>
        ///     The volume sub-path (if any).
        /// </summary>
        [DataMember(Name="subPath", EmitDefaultValue=false)]
        public string SubPath { get; set; }
    }
}
