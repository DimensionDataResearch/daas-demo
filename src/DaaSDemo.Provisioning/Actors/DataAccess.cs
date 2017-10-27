using Akka;
using Akka.Actor;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace DaaSDemo.Provisioning.Actors
{
    using Data;
    using Data.Models;
    using Messages;

    /// <summary>
    ///     Actor that provides access to the master DaaS database.
    /// </summary>
    public class DataAccess
        : ReceiveActorEx
    {
        /// <summary>
        ///     The default name for the actor.
        /// </summary>
        public static readonly string ActorName = "data-access";

        /// <summary>
        ///     <see cref="TenantServerManager"/> actors, keyed by tenant Id.
        /// </summary>
        readonly Dictionary<int, IActorRef> _serverManagers = new Dictionary<int, IActorRef>();

        /// <summary>
        ///     Mappings from node internal IP addresses to external IP addresses.
        /// </summary>
        ImmutableDictionary<string, string> _nodeIPAddressMappings = ImmutableDictionary<string, string>.Empty;

        /// <summary>
        ///     Create a new <see cref="DataAccess"/>.
        /// </summary>
        public DataAccess()
        {
            ReceiveAsync<Command>(async command =>
            {
                switch (command)
                {
                    case Command.ScanTenants:
                    {
                        await ScanTenants();

                        break;
                    }
                    case Command.ScanIPAddressMappings:
                    {
                        await ScanIPAddressMappings();

                        break;
                    }
                    default:
                    {
                        Unhandled(command);

                        break;
                    }
                }
            });
            ReceiveAsync<ServerStatusChanged>(UpdateServerProvisioningStatus);
            ReceiveAsync<ServerIngressChanged>(UpdateServerIngress);
            Receive<Terminated>(terminated =>
            {
                int? serverId =
                    _serverManagers.Where(
                        entry => Equals(entry.Value, terminated.ActorRef)
                    )
                    .Select(
                        entry => (int?)entry.Key
                    )
                    .FirstOrDefault();

                if (serverId.HasValue)
                    _serverManagers.Remove(serverId.Value); // Server manager terminated.
                else
                    Unhandled(terminated);
            });
        }

        /// <summary>
        ///     Called when the actor is started.
        /// </summary>
        protected override void PreStart()
        {
            Context.System.Scheduler.ScheduleTellRepeatedly(
                initialDelay: TimeSpan.Zero,
                interval: TimeSpan.FromMinutes(5),
                receiver: Self,
                message: Command.ScanIPAddressMappings,
                sender: Self
            );

            Context.System.Scheduler.ScheduleTellRepeatedly(
                initialDelay: TimeSpan.Zero,
                interval: TimeSpan.FromSeconds(5),
                receiver: Self,
                message: Command.ScanTenants,
                sender: Self
            );
        }

        /// <summary>
        ///     Scan the database for changes to tenants (their servers and / or databases).
        /// </summary>
        /// <returns>
        ///     A <see cref="Task"/> representing the operation.
        /// </returns>
        async Task ScanTenants()
        {
            Log.Info("Scanning tenants...");

            DatabaseServer[] servers;
            using (Entities entities = CreateEntityContext())
            {
                servers = await entities.DatabaseServers.Include(server => server.Databases).ToArrayAsync();
            }

            foreach (DatabaseServer server in servers)
            {
                Log.Info("Discovered database server {ServerId} (Name:{ServerName}) owned by tenant {TenantId}.",
                    server.Id,
                    server.Name,
                    server.TenantId
                );

                IActorRef serverManager;
                if (!_serverManagers.TryGetValue(server.TenantId, out serverManager))
                {
                    serverManager = Context.ActorOf(
                        Props.Create(() => new TenantServerManager(server.Id, Self)),
                        name: TenantServerManager.ActorName(server.TenantId)
                    );
                    _serverManagers.Add(server.TenantId, serverManager);

                    Log.Info("Created TenantServerManager {ActorName} for server {ServerId} (Tenant:{TenantId}).",
                        serverManager.Path.Name,
                        server.Id,
                        server.TenantId
                    );
                }

                Log.Info("Notifying TenantServerManager {ActorName} of current configuration for server {ServerId}.", serverManager.Path.Name, server.Id);
                serverManager.Tell(server);
            }

            Log.Info("Tenant scan complete.");
        }

        /// <summary>
        ///     Scan the database for changes to IP address mappings for Kubernetes nodes.
        /// </summary>
        /// <returns>
        ///     A <see cref="Task"/> representing the operation.
        /// </returns>
        async Task ScanIPAddressMappings()
        {
            Log.Info("Scanning IP address mappings...");

            IPAddressMapping[] mappings;
            using (Entities entities = CreateEntityContext())
            {
                mappings = await entities.IPAddressMappings.ToArrayAsync();
            }

            ImmutableDictionary<string, string> previousMappings = _nodeIPAddressMappings;
            ImmutableDictionary<string, string> currentMappings = mappings.ToImmutableDictionary(
                mapping => mapping.InternalIP,
                mapping => mapping.ExternalIP
            );

            HashSet<string> internalIPs = new HashSet<string>(previousMappings.Keys);
            internalIPs.UnionWith(currentMappings.Keys);

            bool haveMappingsChanged = false;
            foreach (string internalIP in internalIPs)
            {
                string previousExternalIP;
                previousMappings.TryGetValue(internalIP, out previousExternalIP);

                string externalIP;
                currentMappings.TryGetValue(internalIP, out externalIP);
                
                if (!String.Equals(externalIP, previousExternalIP, StringComparison.OrdinalIgnoreCase))
                {
                    haveMappingsChanged = true;

                    break;
                }
            }

            if (haveMappingsChanged)
            {
                Log.Info("One or more node IP address mappings have changed; publishing changes...");

                foreach (IActorRef serverManager in _serverManagers.Values)
                {
                    serverManager.Tell(
                        new IPAddressMappingsChanged(currentMappings)
                    );
                }

                Log.Info("Published changes to node IP address mappings.");
            }

            Log.Info("IP address mapping scan complete.");
        }

        /// <summary>
        ///     Update server provisioning status.
        /// </summary>
        /// <param name="serverIngressChanged">
        ///     A <see cref="ServerStatusChanged"/> message describing the change.
        /// </param>
        /// <returns>
        ///     A <see cref="Task"/> representing the operation.
        /// </returns>
        async Task UpdateServerProvisioningStatus(ServerStatusChanged serverProvisioningNotification)
        {
            if (serverProvisioningNotification == null)
                throw new ArgumentNullException(nameof(serverProvisioningNotification));
            
            using (Entities entities = CreateEntityContext())
            {
                DatabaseServer server = await entities.DatabaseServers.FindAsync(serverProvisioningNotification.ServerId);
                if (server == null)
                {
                    Log.Warning("Received ServerIngressChanged notification for non-existent server (Id:{ServerId}).",
                        serverProvisioningNotification.ServerId
                    );

                    return;
                }

                server.Status = serverProvisioningNotification.Status;
                switch (server.Status)
                {
                    case ProvisioningStatus.Ready:
                    case ProvisioningStatus.Error:
                    {
                        server.Action = ProvisioningAction.None;

                        break;
                    }
                }

                await entities.SaveChangesAsync();
            }
        }

        /// <summary>
        ///     Update server ingress details.
        /// </summary>
        /// <param name="serverIngressChanged">
        ///     A <see cref="ServerIngressChanged"/> message describing the change.
        /// </param>
        /// <returns>
        ///     A <see cref="Task"/> representing the operation.
        /// </returns>
        async Task UpdateServerIngress(ServerIngressChanged serverIngressChanged)
        {
            if (serverIngressChanged == null)
                throw new ArgumentNullException(nameof(serverIngressChanged));
            
            using (Entities entities = CreateEntityContext())
            {
                DatabaseServer server = await entities.DatabaseServers.FirstOrDefaultAsync(
                    matchServer => matchServer.Id == serverIngressChanged.ServerId
                );
                if (server == null)
                {
                    Log.Warning("Received ServerIngressChanged notification for non-existent server (Id:{ServerId}).",
                        serverIngressChanged.ServerId
                    );

                    return;
                }

                server.IngressIP = serverIngressChanged.IngressIP;
                server.IngressPort = serverIngressChanged.IngressPort;

                await entities.SaveChangesAsync();
            }
        }

        /// <summary>
        ///     Create an entity data context.
        /// </summary>
        /// <returns>
        ///     The configured context.
        /// </returns>
        Entities CreateEntityContext()
        {
            DbContextOptionsBuilder optionsBuilder = new DbContextOptionsBuilder()
                .UseSqlServer(
                    connectionString: Context.System.Settings.Config.GetString("daas.db.connection-string")
                );

            return new Entities(optionsBuilder.Options);
        }

        /// <summary>
        ///     Database watcher commands.
        /// </summary>
        public enum Command
        {
            /// <summary>
            ///     Scan the database for changes to tenants (their servers and databases).
            /// </summary>
            ScanTenants,

            /// <summary>
            ///     Scan the database for changes to IP address mappings (for Kubernetes nodes).
            /// </summary>
            ScanIPAddressMappings
        }
    }
}