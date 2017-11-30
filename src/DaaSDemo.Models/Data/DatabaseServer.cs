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
        ///     The server configuration.
        /// </summary>
        [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Auto)]
        public DatabaseServerSettings Settings { get; set; } = new DatabaseServerSettings();

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

                Settings = Settings.Clone(),

                PublicFQDN = PublicFQDN,
                PublicPort = PublicPort,

                Action = Action,
                Phase = Phase,
                Status = Status,

                Events = Events.Select(evt => evt.Clone()).ToList(),
                
                DatabaseIds = new HashSet<string>(DatabaseIds),
            };
        }
    }
}
