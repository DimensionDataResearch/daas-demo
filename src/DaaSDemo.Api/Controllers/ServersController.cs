using HTTPlease;
using HTTPlease.Formatters;
using HTTPlease.Formatters.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Raven.Client.Documents;
using Raven.Client.Documents.Session;
using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

using StatusCodes = Microsoft.AspNetCore.Http.StatusCodes;

namespace DaaSDemo.Api.Controllers
{
    using Data;
    using Data.Indexes;
    using Models.Api;
    using Models.Data;

    /// <summary>
    ///     Controller for the servers API.
    /// </summary>
    [Route("api/v1/servers")]
    public class ServersController
        : Controller
    {
        /// <summary>
        ///     Create a new servers API controller.
        /// </summary>
        /// <param name="documentSession">
        ///     The RavenDB document session for the current request.
        /// </param>
        /// <param name="logger">
        ///     The controller's log facility.
        /// </param>
        public ServersController(IDocumentSession documentSession, ILogger<ServersController> logger)
        {
            if (documentSession == null)
                throw new ArgumentNullException(nameof(documentSession));

            if (logger == null)
                throw new ArgumentNullException(nameof(logger));
            
            DocumentSession = documentSession;
            Log = logger;
        }

        /// <summary>
        ///     The RavenDB document session for the current request.
        /// </summary>
        IDocumentSession DocumentSession { get; }

        /// <summary>
        ///     The controller's log facility.
        /// </summary>
        ILogger Log { get; }

        /// <summary>
        ///     Get all servers.
        /// </summary>
        [HttpGet]
        public IActionResult List()
        {
            return Json(
                DocumentSession.Query<DatabaseServer, DatabaseServerDetails>()
                    .ProjectFromIndexFieldsInto<DatabaseServerDetail>()
            );
        }

        /// <summary>
        ///     Get a server by Id.
        /// </summary>
        /// <param name="serverId">
        ///     The server Id.
        /// </param>
        [HttpGet("{serverId}")]
        public IActionResult GetById(string serverId)
        {
            DatabaseServer server = DocumentSession
                .Include<DatabaseServer>(databaseServer => databaseServer.TenantId)
                .Load<DatabaseServer>(serverId);
            if (server == null)
            {
                return NotFound(new
                {
                    Id = serverId,
                    EntityType = "DatabaseServer",
                    Message = $"No server found with Id {serverId}."
                });
            }

            Tenant tenant = DocumentSession.Load<Tenant>(server.TenantId);
            if (tenant == null)
            {
                return NotFound(new
                {
                    Id = server.TenantId,
                    EntityType = "Tenant",
                    Message = $"No tenant found with Id {server.TenantId} (referenced by server {serverId})."
                });
            }

            return Json(
                new DatabaseServerDetail(server, tenant)
            );
        }

        /// <summary>
        ///     Provision a database server for a tenant.
        /// </summary>
        /// <param name="newDatabaseServer">
        ///     The request body as a <see cref="NewDatabaseServer"/>.
        /// </param>
        [HttpPost]
        public IActionResult CreateServer([FromBody] NewDatabaseServer newDatabaseServer)
        {
            if (newDatabaseServer == null)
            {
                return BadRequest(new
                {
                    EntityType = "DatabaseServer",
                    Reason = "InvalidRequest",
                    Message = "Must supply database details in the request body."
                });
            }

            switch (newDatabaseServer.Kind)
            {
                case DatabaseServerKind.SqlServer:
                {
                    break;
                }
                case DatabaseServerKind.RavenDB:
                {
                    return BadRequest(new
                    {
                        EntityType = "DatabaseServer",
                        Reason = "NotImplemented",
                        Message = "RavenDB servers are not supported yet."
                    });
                }
                default:
                {
                    return BadRequest(new
                    {
                        EntityType = "DatabaseServer",
                        Reason = "InvalidServerType",
                        Message = $"Unsupported server type '{newDatabaseServer.Kind}'."
                    });
                }
            }

            Tenant tenant = DocumentSession.Load<Tenant>(newDatabaseServer.TenantId);
            if (tenant == null)
            {
                return NotFound(new
                {
                    Id = newDatabaseServer.TenantId,
                    EntityType = "Tenant",
                    Message = $"No tenant found with Id {newDatabaseServer.TenantId}."
                });
            }

            // TODO: Support for multiple servers.
            DatabaseServer existingServer = DocumentSession.Query<DatabaseServer>().FirstOrDefault(server => server.TenantId == tenant.Id);
            if (existingServer != null)
            {
                return StatusCode(StatusCodes.Status409Conflict, new
                {
                    TenantId = existingServer,
                    EntityType = "DatabaseServer",
                    Message = $"Database server {existingServer.Id} already exists for tenant with Id {tenant.Id}",
                    ExistingServer = existingServer
                });
            }

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var databaseServer = new DatabaseServer
            {
                Name = newDatabaseServer.Name,
                Kind = newDatabaseServer.Kind,
                AdminPassword = newDatabaseServer.AdminPassword,
                Storage =
                {
                    SizeMB = newDatabaseServer.SizeMB
                },
                TenantId = tenant.Id,
                Action = ProvisioningAction.Provision,
                Status = ProvisioningStatus.Pending
            };
            DocumentSession.Store(databaseServer);
            DocumentSession.SaveChanges();

            return StatusCode(StatusCodes.Status202Accepted, new
            {
                Id = databaseServer.Id,
                Name = databaseServer.Name,
                Message = $"Database server {databaseServer.Id} queued for creation."
            });
        }

        /// <summary>
        ///     Reconfigure / repair a database server.
        /// </summary>
        /// <param name="serverId">
        ///     The tenant Id.
        /// </param>
        [HttpPost("{serverId}/reconfigure")]
        public IActionResult ReconfigureServer(string serverId)
        {
            DatabaseServer targetServer = DocumentSession.GetDatabaseServerById(serverId);
            if (targetServer == null)
            {
                return NotFound(new
                {
                    Id = serverId,
                    EntityType = "DatabaseServer",
                    Message = $"No database server found with Id '{serverId}'."
                });
            }

            if (targetServer.Action != ProvisioningAction.None)
            {
                return StatusCode(StatusCodes.Status409Conflict, new
                {
                    Id = targetServer.Id,
                    TenantId = serverId,
                    EntityType = "DatabaseServer",
                    RequestedAction = ProvisioningAction.Reconfigure,
                    Action = targetServer.Action,
                    Status = targetServer.Status,
                    Message = $"Cannot reconfigure database server {targetServer.Id} because another server-level action is already in progress."
                });
            }

            targetServer.Action = ProvisioningAction.Reconfigure;
            targetServer.Status = ProvisioningStatus.Pending;
            DocumentSession.SaveChanges();

            return StatusCode(StatusCodes.Status202Accepted, new
            {
                Id = serverId,
                Message = $"Database server {targetServer.Id} queued for reconfiguration.",
                EntityType = "DatabaseServer"
            });
        }

        /// <summary>
        ///     Deprovision a tenant's database server.
        /// </summary>
        /// <param name="serverId">
        ///     The server Id.
        /// </param>
        [HttpDelete("{serverId}")]
        public IActionResult DestroyServer(string serverId)
        {
            DatabaseServer targetServer = DocumentSession.GetDatabaseServerById(serverId);
            if (targetServer == null)
            {
                return NotFound(new
                {
                    Id = serverId,
                    EntityType = "DatabaseServer",
                    Message = $"No database server found with Id '{serverId}'."
                });
            }

            if (DocumentSession.DoesServerHaveDatabases(targetServer.Id))
            {
                return StatusCode(StatusCodes.Status400BadRequest, new
                {
                    Id = targetServer.Id,
                    TenantId = serverId,
                    EntityType = "DatabaseServer",
                    RequestedAction = ProvisioningAction.Deprovision,
                    Action = targetServer.Action,
                    Status = targetServer.Status,
                    Message = $"Cannot de-provision database server {targetServer.Id} because it still hosts one or more databases. First de-provision these databases and then retry the operation."
                });
            }

            if (targetServer.Action != ProvisioningAction.None)
            {
                return StatusCode(StatusCodes.Status409Conflict, new
                {
                    Id = targetServer.Id,
                    TenantId = serverId,
                    EntityType = "DatabaseServer",
                    RequestedAction = ProvisioningAction.Deprovision,
                    Action = targetServer.Action,
                    Status = targetServer.Status,
                    Message = $"Cannot de-provision database server {targetServer.Id} because an action ({targetServer.Action}) is already in progress for this server."
                });
            }

            targetServer.Action = ProvisioningAction.Deprovision;
            targetServer.Status = ProvisioningStatus.Pending;
            DocumentSession.SaveChanges();

            return StatusCode(StatusCodes.Status202Accepted, new
            {
                Id = serverId,
                Message = $"Database server {targetServer.Id} queued for deletion.",
                EntityType = "DatabaseServer"
            });
        }

        /// <summary>
        ///     Get all servers hosted by the specified server.
        /// </summary>
        /// <param name="serverId">
        ///     The server Id.
        /// </param>
        [HttpGet("{serverId}/databases")]
        public IActionResult GetDatabasesByServer(string serverId)
        {
            return Json(
                DocumentSession.Query<DatabaseInstance, DatabaseInstanceDetails>()
                    .Where(database => database.ServerId == serverId)
                    .ProjectFromIndexFieldsInto<DatabaseInstanceDetail>()
            );
        }
    }
}
