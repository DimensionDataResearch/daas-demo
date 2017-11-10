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
    using Messages;
    using Models.Data;

    // TODO: Supervision policy for TenantServerManager actors (incremental back-off, fail after 5 attempts).

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
        ///     References to management actors for tenant servers, keyed by tenant Id.
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
                    case Command.ScanTenants:
                    {
                        await ScanTenants();

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
            ReceiveAsync<DatabaseStatusChanged>(UpdateDatabaseProvisioningStatus);
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
            Log.Debug("Scanning tenants...");

            DatabaseServer[] servers;
            using (Entities entities = CreateEntityContext())
            {
                servers = await entities.DatabaseServers.Include(server => server.Databases).ToArrayAsync();
            }

            foreach (DatabaseServer server in servers)
            {
                Log.Debug("Discovered database server {ServerId} (Name:{ServerName}) owned by tenant {TenantId}.",
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
                    Context.Watch(serverManager);
                    
                    _serverManagers.Add(server.TenantId, serverManager);

                    Log.Info("Created TenantServerManager {ActorName} for server {ServerId} (Tenant:{TenantId}).",
                        serverManager.Path.Name,
                        server.Id,
                        server.TenantId
                    );
                }

                Log.Debug("Notifying TenantServerManager {ActorName} of current configuration for server {ServerId}.", serverManager.Path.Name, server.Id);
                serverManager.Tell(server);
            }

            Log.Debug("Tenant scan complete.");
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

            Log.Debug("Updating provisioning status for server {ServerId} due to {NotificationType} notification...",
                serverProvisioningNotification.ServerId,
                serverProvisioningNotification.GetType().Name
            );

            using (Entities entities = CreateEntityContext())
            {
                DatabaseServer server = await entities.DatabaseServers.FindAsync(serverProvisioningNotification.ServerId);
                if (server == null)
                {
                    Log.Warning("Received ServerStatusChanged notification for non-existent server (Id:{ServerId}).",
                        serverProvisioningNotification.ServerId
                    );

                    return;
                }

                Log.Debug("Existing provisioning status for server {ServerId} is {Action}:{Status}:{Phase}.",
                    server.Id,
                    server.Action,
                    server.Status,
                    server.Phase
                );

                if (serverProvisioningNotification.Status.HasValue)
                {
                    server.Status = serverProvisioningNotification.Status.Value;

                    switch (server.Status)
                    {
                        case ProvisioningStatus.Ready:
                        case ProvisioningStatus.Error:
                        {
                            server.Action = ProvisioningAction.None;
                            server.Phase = ServerProvisioningPhase.None;

                            break;
                        }
                        case ProvisioningStatus.Deprovisioned:
                        {
                            entities.DatabaseServers.Remove(server);

                            break;
                        }
                    }
                }

                if (serverProvisioningNotification.Phase.HasValue)
                    server.Phase = serverProvisioningNotification.Phase.Value;

                Log.Debug("New provisioning status for server {ServerId} is {Action}:{Status}:{Phase}.",
                    server.Id,
                    server.Action,
                    server.Status,
                    server.Phase
                );

                await entities.SaveChangesAsync();
            }

            Log.Debug("Updated provisioning status for server {ServerId}.", serverProvisioningNotification.ServerId);
        }

        /// <summary>
        ///     Update database provisioning status.
        /// </summary>
        /// <param name="databaseIngressChanged">
        ///     A <see cref="DatabaseStatusChanged"/> message describing the change.
        /// </param>
        /// <returns>
        ///     A <see cref="Task"/> representing the operation.
        /// </returns>
        async Task UpdateDatabaseProvisioningStatus(DatabaseStatusChanged databaseProvisioningNotification)
        {
            if (databaseProvisioningNotification == null)
                throw new ArgumentNullException(nameof(databaseProvisioningNotification));
            
            using (Entities entities = CreateEntityContext())
            {
                DatabaseInstance database = await entities.DatabaseInstances.FindAsync(databaseProvisioningNotification.DatabaseId);
                if (database == null)
                {
                    Log.Warning("Received DatabaseStatusChanged notification for non-existent database (Id:{DatabaseId}).",
                        databaseProvisioningNotification.DatabaseId
                    );

                    return;
                }

                database.Status = databaseProvisioningNotification.Status;
                switch (database.Status)
                {
                    case ProvisioningStatus.Ready:
                    case ProvisioningStatus.Error:
                    {
                        database.Action = ProvisioningAction.None;

                        break;
                    }
                    case ProvisioningStatus.Deprovisioned:
                    {
                        entities.DatabaseInstances.Remove(database);

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

                server.PublicFQDN = serverIngressChanged.PublicFQDN;
                server.PublicPort = serverIngressChanged.PublicPort;

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
            ScanTenants = 1
        }
    }
}
