using Newtonsoft.Json;
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
        /// <param name="server">
        ///     The underlying <see cref="DatabaseServer"/>.
        /// </param>
        /// <param name="tenant">
        ///     The server's owning <see cref="Tenant"/>.
        /// </param>
        public DatabaseServerDetail(DatabaseServer server, Tenant tenant)
        {
            if (server == null)
                throw new ArgumentNullException(nameof(server));
            
            if (tenant == null)
                throw new ArgumentNullException(nameof(tenant));

            Id = server.Id;
            Name = server.Name;
            Kind = server.Kind;

            PublicFQDN = server.PublicFQDN;
            PublicPort = server.PublicPort;
            
            StorageMB = server.Storage.SizeMB;
            
            Action = server.Action;
            Phase = server.Phase;
            Status = server.Status;
            
            TenantId = tenant.Id;
            TenantName = tenant.Name;
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
        ///     The total amount of storage (in MB) allocated to the server.
        /// </summary>
        public int StorageMB { get; set; }

        /// <summary>
        ///     The server's currently-requested provisioning action (if any).
        /// </summary>
        public ProvisioningAction Action { get; set; }

        /// <summary>
        ///     The server's current provisioning phase (if any).
        /// </summary>
        public ServerProvisioningPhase Phase { get; set; }

        /// <summary>
        ///     The server's provisioning status.
        /// </summary>
        public ProvisioningStatus Status { get; set; }
    }
}
