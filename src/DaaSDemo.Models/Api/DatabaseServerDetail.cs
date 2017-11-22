using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DaaSDemo.Models.Api
{
    using Models.Data;

    /// <summary>
    ///     Detailed information about a server server.
    /// </summary>
    public class DatabaseServerDetail
    {
        /// <summary>
        ///     Create new <see cref="DatabaseServerDetail"/>.
        /// </summary>
        public DatabaseServerDetail()
        {
        }

        /// <summary>
        ///     Create new <see cref="DatabaseServerDetail"/>.
        /// </summary>
        /// <param name="serverId"></param>
        /// <param name="serverName"></param>
        /// <param name="kind"></param>
        /// <param name="publicFQDN"></param>
        /// <param name="publicPort"></param>
        /// <param name="serverAction"></param>
        /// <param name="phase"></param>
        /// <param name="serverStatus"></param>
        /// <param name="tenantId"></param>
        /// <param name="tenantName"></param>
        public DatabaseServerDetail(string serverId, string serverName, DatabaseServerKind kind, string publicFQDN, int? publicPort, ProvisioningAction serverAction, ServerProvisioningPhase phase, ProvisioningStatus serverStatus, string tenantId, string tenantName)
        {
            Id = serverId;
            Name = serverName;
            Kind = kind;
            PublicFQDN = publicFQDN;
            PublicPort = publicPort;
            Action = serverAction;
            Phase = phase;
            Status = serverStatus;
            TenantId = tenantId;
            TenantName = tenantName;
        }

        /// <summary>
        ///     The server Id.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        ///     The server name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///     The type of server (e.g. SQL Server or RavenDB).
        /// </summary>
        public DatabaseServerKind Kind { get; set; }

        /// <summary>
        ///     The server's public TCP port.
        /// </summary>
        public string PublicFQDN { get; set; }

        /// <summary>
        ///     The server's fully-qualified public domain name.
        /// </summary>
        public int? PublicPort { get; set; }

        /// <summary>
        ///     The Id of the tenant that owns the server.
        /// </summary>
        public string TenantId { get; set; }

        /// <summary>
        ///     The name of the tenant that owns the server.
        /// </summary>
        public string TenantName { get; set; }

        /// <summary>
        ///     The server's currently-requested provisioning action (if any).
        /// </summary>
        public ProvisioningAction Action { get; set; }

        /// <summary>
        ///     The server's currentl provisioning phase (if any).
        /// </summary>
        public ServerProvisioningPhase Phase { get; set; }

        /// <summary>
        ///     The server's provisioning status.
        /// </summary>
        public ProvisioningStatus Status { get; set; }
    }
}
