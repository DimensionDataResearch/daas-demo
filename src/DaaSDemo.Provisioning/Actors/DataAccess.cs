using Akka;
using Akka.Actor;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
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
        ///     Create a new <see cref="DataAccess"/>.
        /// </summary>
        public DataAccess()
        {
            ReceiveAsync<Command>(async command =>
            {
                switch (command)
                {
                    case Command.ScanDatabase:
                    {
                        await ScanDatabase();

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
                interval: TimeSpan.FromSeconds(5),
                receiver: Self,
                message: Command.ScanDatabase,
                sender: Self
            );
        }

        /// <summary>
        ///     Scan the database for changes.
        /// </summary>
        /// <returns>
        ///     A <see cref="Task"/> representing the operation.
        /// </returns>
        async Task ScanDatabase()
        {
            Log.Info("Scanning database...");

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

            Log.Info("Database scan complete.");
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
            ///     Scan the database for changes.
            /// </summary>
            ScanDatabase
        }
    }
}