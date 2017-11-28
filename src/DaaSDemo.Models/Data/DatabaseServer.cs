using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace DaaSDemo.Models.Data
{
    /// <summary>
    ///     Represents a database server allocated to a tenant.
    /// </summary>
    [EntitySet("DatabaseServer")]
    public class DatabaseServer
        : IDeepCloneable<DatabaseServer>
    {
        /// <summary>
        ///     The server Id.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        ///     The server (container / deployment) name.
        /// </summary>
        [MaxLength(200)]
        [Required(AllowEmptyStrings = false)]
        public string Name { get; set; }

        /// <summary>
        ///     The type of server (e.g. SQL Server or RavenDB).
        /// </summary>
        [Required]
        public DatabaseServerKind Kind { get; set; }

        /// <summary>
        ///     The server's administrative ("sa" user) password (if required by the server kind).
        /// </summary>
        public string AdminPassword { get; set; }

        /// <summary>
        ///     The external IP address that the server is listening on.
        /// </summary>
        [MaxLength(100)]
        public string PublicFQDN { get ; set; }

        /// <summary>
        ///     The external port that the server is listening on.
        /// </summary>
        public int? PublicPort { get; set; }

        /// <summary>
        ///     The Id of the tenant that owns the database server.
        /// </summary>
        [Required]
        public string TenantId { get; set; }

        /// <summary>
        ///     The desired action for the server.
        /// </summary>
        public ProvisioningAction Action { get; set; } = ProvisioningAction.None;

        /// <summary>
        ///     The current status of the server.
        /// </summary>
        public ProvisioningStatus Status { get; set; } = ProvisioningStatus.Pending;

        /// <summary>
        ///     The server's current provisioning phase.
        /// </summary>
        public ServerProvisioningPhase Phase { get; set; } = ServerProvisioningPhase.None;

        /// <summary>
        ///     The server's storage configuration.
        /// </summary>
        [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Reuse)]
        public DatabaseServerStorage Storage { get; } = new DatabaseServerStorage();

        /// <summary>
        ///     Events relating to the server.
        /// </summary>
        [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Reuse)]
        public List<DatabaseServerEvent> Events { get; private set; } = new List<DatabaseServerEvent>();

        /// <summary>
        ///     The Ids of databases hosted on the server.
        /// </summary>
        public HashSet<string> DatabaseIds { get; private set; } = new HashSet<string>();

        /// <summary>
        ///     Create a deep clone of the <see cref="DatabaseServer"/>.
        /// </summary>
        /// <returns>
        ///     The cloned <see cref="DatabaseServer"/>.
        /// </returns>
        public DatabaseServer Clone()
        {
            return new DatabaseServer
            {
                Id = Id,
                TenantId = TenantId,

                Name = Name,
                Kind = Kind,

                AdminPassword = AdminPassword,
                PublicFQDN = PublicFQDN,
                PublicPort = PublicPort,

                Action = Action,
                Phase = Phase,
                Status = Status,

                Storage =
                {
                    SizeMB = Storage.SizeMB
                },

                Events = Events.Select(evt => evt.Clone()).ToList(),
                
                DatabaseIds = new HashSet<string>(DatabaseIds),
            };
        }

        /// <summary>
        ///     Add a provisioning event by capturing the current server state.
        /// </summary>
        /// <param name="messages">
        ///     Messages (if any) to associate with the event.
        /// </param>
        public void AddProvisioningEvent(params string[] messages)
            => AddProvisioningEvent((IEnumerable<string>)messages);

        /// <summary>
        ///     Add a provisioning event by capturing the current server state.
        /// </summary>
        /// <param name="messages">
        ///     Messages (if any) to associate with the event.
        /// </param>
        public void AddProvisioningEvent(IEnumerable<string> messages)
        {
            if (messages == null)
                throw new ArgumentNullException(nameof(messages));

            var provisioningEvent = new DatabaseServerProvisioningEvent
            {
                Timestamp = DateTimeOffset.Now,
                Action = Action,
                Phase = Phase,
                Status = Status,
            };
            provisioningEvent.Messages.AddRange(messages);
            Events.Add(provisioningEvent);
        }

        /// <summary>
        ///     Add an ingress-change event by capturing the current server state.
        /// </summary>
        public void AddIngressChangedEvent()
        {
            var ingressChangedEvent = new DatabaseServerIngressChangedEvent
            {
                Timestamp = DateTimeOffset.Now,
                PublicFQDN = PublicFQDN,
                PublicPort = PublicPort
            };
            if (PublicFQDN == null || PublicPort == null)
                ingressChangedEvent.Messages.Add("Server is not externally-accessible.");
            else
                ingressChangedEvent.Messages.Add($"Server is externally-accessible on '{ingressChangedEvent.PublicFQDN}:{ingressChangedEvent.PublicPort}'.");

            Events.Add(ingressChangedEvent);
        }
    }

    /// <summary>
    ///     Well-known types of event relating to a <see cref="DatabaseServer"/>.
    /// </summary>
    public enum DatabaseServerEventKind
    {
        /// <summary>
        ///     A provisioning-related event.
        /// </summary>
        Provisioning,

        /// <summary>
        ///     Event indicating that a server's ingress details have changed.
        /// </summary>
        IngressChanged
    }

    /// <summary>
    ///     The base class for events relating to a <see cref="DatabaseServer"/>.
    /// </summary>
    public abstract class DatabaseServerEvent
        : IDeepCloneable<DatabaseServerEvent>
    {
        /// <summary>
        ///     The date / time that the event occurred.
        /// </summary>
        public DateTimeOffset Timestamp { get; set; }

        /// <summary>
        ///     Messages (if any) relating to the event.
        /// </summary>
        public List<string> Messages { get; protected set; } = new List<string>();

        /// <summary>
        ///     The kind of event represented by the <see cref="DatabaseServerEvent"/>.
        /// </summary>
        public abstract DatabaseServerEventKind Kind { get; }

        /// <summary>
        ///     Perform a deep clone of the <see cref="DatabaseServerEvent"/>.
        /// </summary>
        /// <returns>
        ///     The cloned <see cref="DatabaseServerEvent"/>.
        /// </returns>
        public abstract DatabaseServerEvent Clone();
    }

    /// <summary>
    ///     A provisioning event related to a <see cref="DatabaseServer"/>.
    /// </summary>
    public class DatabaseServerProvisioningEvent
        : DatabaseServerEvent
    {
        /// <summary>
        ///     The requested action.
        /// </summary>
        public ProvisioningAction Action { get; set; }

        /// <summary>
        ///     The current phase (if any).
        /// </summary>
        public ServerProvisioningPhase Phase { get; set; }

        /// <summary>
        ///     The current status.
        /// </summary>
        public ProvisioningStatus Status { get; set; }

        /// <summary>
        ///     The kind of event represented by the <see cref="DatabaseServerEvent"/>.
        /// </summary>
        public override DatabaseServerEventKind Kind => DatabaseServerEventKind.Provisioning;

        /// <summary>
        ///     Perform a deep clone of the <see cref="DatabaseServerEvent"/>.
        /// </summary>
        /// <returns>
        ///     The cloned <see cref="DatabaseServerEvent"/>.
        /// </returns>
        public override DatabaseServerEvent Clone()
        {
            return new DatabaseServerProvisioningEvent
            {
                Timestamp = Timestamp,
                Messages = new List<string>(Messages),

                Action = Action,
                Phase = Phase,
                Status = Status,
            };
        }
    }

    /// <summary>
    ///     Event indicating that a <see cref="DatabaseServer"/>'s ingress details have changed.
    /// </summary>
    public class DatabaseServerIngressChangedEvent
        : DatabaseServerEvent
    {
        /// <summary>
        ///     The server's current fully-qualified public domain name (if any).
        /// </summary>
        public string PublicFQDN { get; set; }

        /// <summary>
        ///     The server's current public TCP port (if any).
        /// </summary>
        public int? PublicPort { get; set; }

        /// <summary>
        ///     The kind of event represented by the <see cref="DatabaseServerEvent"/>.
        /// </summary>
        public override DatabaseServerEventKind Kind => DatabaseServerEventKind.IngressChanged;

        /// <summary>
        ///     Perform a deep clone of the <see cref="DatabaseServerEvent"/>.
        /// </summary>
        /// <returns>
        ///     The cloned <see cref="DatabaseServerEvent"/>.
        /// </returns>
        public override DatabaseServerEvent Clone()
        {
            return new DatabaseServerIngressChangedEvent
            {
                Timestamp = Timestamp,
                Messages = new List<string>(Messages),

                PublicFQDN = PublicFQDN,
                PublicPort = PublicPort
            };
        }
    }
}
