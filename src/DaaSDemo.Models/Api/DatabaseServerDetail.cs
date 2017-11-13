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
        /// <param name="PublicFQDN"></param>
        /// <param name="PublicPort"></param>
        /// <param name="serverAction"></param>
        /// <param name="phase"></param>
        /// <param name="serverStatus"></param>
        /// <param name="tenantId"></param>
        /// <param name="tenantName"></param>
        public DatabaseServerDetail(int serverId, string serverName, string publicFQDN, int? publicPort, ProvisioningAction serverAction, ServerProvisioningPhase phase, ProvisioningStatus serverStatus, int tenantId, string tenantName)
        {
            Id = serverId;
            Name = serverName;
            PublicFQDN = publicFQDN;
            PublicPort = publicPort;
            Action = serverAction;
            Phase = phase;
            Status = serverStatus;
            TenantId = tenantId;
            TenantName = tenantName;
        }

        /// <summary>
        ///     The tenant Id.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        ///     The server name.
        /// </summary>
        public string Name { get; set; }

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
        public int TenantId { get; set; }

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
