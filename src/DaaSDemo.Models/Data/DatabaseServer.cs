using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace DaaSDemo.Models.Data
{
    /// <summary>
    ///     Represents a database server allocated to a tenant.
    /// </summary>
    [EntitySet("database-server")]
    public class DatabaseServer
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
        ///     The server's administrative ("sa" user) password.
        /// </summary>
        [Required(AllowEmptyStrings = false)]
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
        ///     The name of the tenant that owns the database server.
        /// </summary>
        public string TenantName { get; set; }

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
        ///     The Ids of databases hosted on the server.
        /// </summary>
        public HashSet<string> DatabaseIds { get; } = new HashSet<string>();

        /// <summary>
        ///     Get the connection string for the DaaS master database.
        /// </summary>
        /// <returns>
        ///     The connection string.
        /// </returns>
        public string GetMasterConnectionString() => $"Data Source={PublicFQDN},{PublicPort};Initial Catalog=master;User=sa;Password={AdminPassword}";
    }
}
