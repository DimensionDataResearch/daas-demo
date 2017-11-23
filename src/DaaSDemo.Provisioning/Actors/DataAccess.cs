using Akka;
using Akka.Actor;
using Akka.DI.Core;
using Microsoft.Extensions.Options;
using Raven.Client.Documents;
using Raven.Client.Documents.Session;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace DaaSDemo.Provisioning.Actors
{
    using Common.Options;
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
        ///     References to management actors for tenant servers, keyed by server Id.
        /// </summary>
        readonly Dictionary<string, IActorRef> _serverManagers = new Dictionary<string, IActorRef>();

        /// <summary>
        ///     Create a new <see cref="DataAccess"/>.
        /// </summary>
        /// <param name="documentStore">
        ///     The RavenDB document store.
        /// </param>
        public DataAccess(IDocumentStore documentStore)
        {
            DocumentStore = documentStore;

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
                string serverId =
                    _serverManagers.Where(
                        entry => Equals(entry.Value, terminated.ActorRef)
                    )
                    .Select(
                        entry => entry.Key
                    )
                    .FirstOrDefault();

                if (!String.IsNullOrWhiteSpace(serverId))
                    _serverManagers.Remove(serverId); // Server manager terminated.
                else
                    Unhandled(terminated);
            });
        }

        /// <summary>
        ///     The RavenDB document store.
        /// </summary>
        public IDocumentStore DocumentStore { get; }

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

            using (IAsyncDocumentSession session = DocumentStore.OpenAsyncSession())
            {
                List<DatabaseServer> servers = await session.Query<DatabaseServer>().ToListAsync();

                foreach (DatabaseServer server in servers)
                {
                    Log.Debug("Discovered database server {ServerId} (Name:{ServerName}) owned by tenant {TenantId}.",
                        server.Id,
                        server.Name,
                        server.TenantId
                    );

                    IActorRef serverManager;
                    if (!_serverManagers.TryGetValue(server.Id, out serverManager))
                    {
                        serverManager = Context.ActorOf(
                            Context.DI().Props<TenantServerManager>(),
                            name: TenantServerManager.ActorName(server.Id)
                        );
                        Context.Watch(serverManager);
                        
                        _serverManagers.Add(server.Id, serverManager);

                        serverManager.Tell(new TenantServerManager.Initialize(
                            initialState: server,
                            dataAccess: Self
                        ));

                        Log.Info("Created TenantServerManager {ActorName} for server {ServerId} (Tenant:{TenantId}).",
                            serverManager.Path.Name,
                            server.Id,
                            server.TenantId
                        );
                    }
                    else
                    {
                        Log.Debug("Notifying TenantServerManager {ActorName} of current configuration for server {ServerId}.", serverManager.Path.Name, server.Id);
                        serverManager.Tell(
                            server.Clone()
                        );
                    }

                    Dictionary<string, DatabaseInstance> databases = await session.LoadAsync<DatabaseInstance>(server.DatabaseIds);
                    foreach (string databaseId in databases.Keys)
                    {
                        DatabaseInstance database = databases[databaseId];
                        if (database == null)
                        {
                            Log.Warning("Server {ServerId} in management database refers to non-existent database {DatabaseId}.",
                                server.Id,
                                databaseId
                            );

                            continue;
                        }

                        Log.Debug("Notifying TenantServerManager {ActorName} of current configuration for database {DatabaseId} in server {ServerId}.",
                            serverManager.Path.Name,
                            database.Id,
                            server.Id
                        );
                        serverManager.Tell(
                            database.Clone()
                        );
                    }
                }
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

            using (IAsyncDocumentSession session = DocumentStore.OpenAsyncSession())
            {
                DatabaseServer server = await session.LoadAsync<DatabaseServer>(serverProvisioningNotification.ServerId);
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
                            session.Delete(server);

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

                await session.SaveChangesAsync();
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
            
            using (IAsyncDocumentSession session = DocumentStore.OpenAsyncSession())
            {
                DatabaseInstance database = await session
                    .Include<DatabaseInstance>(db => db.ServerId)
                    .LoadAsync<DatabaseInstance>(databaseProvisioningNotification.DatabaseId);
                if (database == null)
                {
                    Log.Warning("Received DatabaseStatusChanged notification for non-existent database (Id:{DatabaseId}).",
                        databaseProvisioningNotification.DatabaseId
                    );

                    return;
                }

                DatabaseServer server = await session.LoadAsync<DatabaseServer>(database.ServerId);
                if (server == null)
                {
                    Log.Warning("Received DatabaseStatusChanged notification for database in non-existent server (Id:{ServerId}).",
                        database.ServerId
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
                        server.DatabaseIds.Remove(database.Id);
                        session.Delete(database);

                        break;
                    }
                }

                await session.SaveChangesAsync();
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
            
            using (IAsyncDocumentSession session = DocumentStore.OpenAsyncSession())
            {
                DatabaseServer server = await session.LoadAsync<DatabaseServer>(serverIngressChanged.ServerId);
                if (server == null)
                {
                    Log.Warning("Received ServerIngressChanged notification for non-existent server (Id:{ServerId}).",
                        serverIngressChanged.ServerId
                    );

                    return;
                }

                server.PublicFQDN = serverIngressChanged.PublicFQDN;
                server.PublicPort = serverIngressChanged.PublicPort;

                await session.SaveChangesAsync();
            }
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
