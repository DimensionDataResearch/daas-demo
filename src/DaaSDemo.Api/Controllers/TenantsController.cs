using HTTPlease;
using HTTPlease.Formatters;
using HTTPlease.Formatters.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Raven.Client.Documents.Session;
using System;
using System.Collections.Generic;
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
    ///     Controller for the tenants API.
    /// </summary>
    [Route("api/v1/tenants")]
    public class TenantsController
        : Controller
    {
        /// <summary>
        ///     Create a new tenants API controller.
        /// </summary>
        /// <param name="documentSession">
        ///     The RavenDB document session for the current request.
        /// </param>
        /// <param name="logger">
        ///     The controller's log facility.
        /// </param>
        public TenantsController(IDocumentSession documentSession, ILogger<TenantsController> logger)
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
        ///     Get a tenant by Id.
        /// </summary>
        /// <param name="tenantId">
        ///     The tenant Id.
        /// </param>
        [HttpGet("{tenantId}")]
        public IActionResult GetById(string tenantId)
        {
            Tenant tenant = DocumentSession.Load<Tenant>(tenantId);
            if (tenant != null)
                return Json(tenant);

            return NotFound(new
            {
                Id = tenantId,
                EntityType = "Tenant",
                Message = $"No tenant found with Id {tenantId}"
            });
        }

        /// <summary>
        ///     Get all tenants.
        /// </summary>
        [HttpGet]
        public IActionResult List()
        {
            return Json(
                DocumentSession.Query<Tenant>()
            );
        }

        /// <summary>
        ///     Create a tenant.
        /// </summary>
        /// <param name="newTenant">
        ///     The request body as a <see cref="Tenant"/>.
        /// </param>
        [HttpPost]
        public IActionResult Create([FromBody] NewTenant newTenant)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var tenant = new Tenant
            {
                Name = newTenant.Name
            };

            DocumentSession.Store(tenant);
            DocumentSession.SaveChanges();

            return Json(tenant);
        }

        /// <summary>
        ///     Get a tenant's database server.
        /// </summary>
        /// <param name="tenantId">
        ///     The tenant Id.
        /// </param>
        [HttpGet("{tenantId}/server")]
        public IActionResult GetServer(string tenantId)
        {
            Tenant tenant = DocumentSession.Load<Tenant>(tenantId);
            if (tenant == null)
            {
                return NotFound(new
                {
                    Id = tenantId,
                    EntityType = "Tenant",
                    Message = $"No tenant found with Id {tenantId}"
                });
            }

            DatabaseServer server = DocumentSession.Query<DatabaseServer>()
                .FirstOrDefault(databaseServer => databaseServer.TenantId == tenantId);
            if (server == null)
            {
                return NotFound(new
                {
                    TenantId = tenantId,
                    EntityType = "DatabaseServer",
                    Message = $"No database server found for tenant with Id {tenantId}"
                });
            }

            return Json(
                new DatabaseServerDetail(server, tenant)
            );
        }

        /// <summary>
        ///     Get all databases owned by a tenant.
        /// </summary>
        /// <param name="tenantId">
        ///     The tenant Id.
        /// </param>
        [HttpGet("{tenantId}/databases")]
        public IActionResult GetDatabases(string tenantId)
        {
            Tenant tenant = DocumentSession.Load<Tenant>(tenantId);
            if (tenant == null)
            {
                return NotFound(new
                {
                    Id = tenantId,
                    EntityType = "Tenant",
                    Message = $"Tenant not found with Id '{tenantId}."
                });
            }

            return Json(
                DocumentSession.Query<DatabaseInstanceDetail, DatabaseInstanceDetails>()
                    .Where(database => database.TenantId == tenantId)
            );
        }

        /// <summary>
        ///     Provision a database for a tenant.
        /// </summary>
        /// <param name="tenantId">
        ///     The tenant Id.
        /// </param>
        /// <param name="newDatabase">
        ///     The request body as a <see cref="Database"/>.
        /// </param>
        [HttpPost("{tenantId}/databases")]
        public IActionResult CreateDatabase(string tenantId, [FromBody] NewDatabaseInstance newDatabase)
        {
            Tenant tenant = DocumentSession.Load<Tenant>(tenantId);
            if (tenant == null)
            {
                return NotFound(new
                {
                    Id = tenantId,
                    EntityType = "Tenant",
                    Message = $"Tenant not found with Id '{tenantId}."
                });
            }

            if (newDatabase == null)
            {
                return BadRequest(new
                {
                    EntityType = "Database",
                    Message = $"Must supply database details in the request body."
                });
            }

            Tenant ownerTenant = DocumentSession.GetTenantById(tenantId);
            if (ownerTenant == null)
            {
                return NotFound(new
                {
                    Id = tenantId,
                    EntityType = "Tenant",
                    Message = $"No tenant found with Id {tenantId}"
                });
            }

            DatabaseServer targetServer = DocumentSession.GetDatabaseServerByTenantId(tenantId);
            if (targetServer == null)
            {
                return NotFound(new
                {
                    Id = tenantId,
                    EntityType = "DatabaseServer",
                    Message = $"No database server found for tenant with Id {tenantId}"
                });
            }

            if (targetServer.Status != ProvisioningStatus.Ready)
            {
                return StatusCode(StatusCodes.Status409Conflict, new
                {
                    Id = targetServer.Id,
                    TenantId = tenantId,
                    EntityType = "DatabaseServer",
                    Action = targetServer.Action,
                    Status = targetServer.Status,
                    Message = $"Cannot create a database in server {targetServer.Id} because a server-level action is already in progress."
                });
            }

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            DatabaseInstance existingDatabase = DocumentSession.GetDatabaseInstanceByName(newDatabase.Name, targetServer.Id);
            if (existingDatabase != null)
            {
                return StatusCode(StatusCodes.Status409Conflict, new
                {
                    Id = existingDatabase.Id,
                    Name = existingDatabase.Name,
                    EntityType = "Database",
                    Message = $"Database '{existingDatabase.Name}' already exists on server '{targetServer.Name}'."
                });
            }

            var database = new DatabaseInstance
            {
                Name = newDatabase.Name,
                DatabaseUser = newDatabase.DatabaseUser,
                DatabasePassword = newDatabase.DatabasePassword,
                ServerId = targetServer.Id,
                TenantId = ownerTenant.Id,
                Action = ProvisioningAction.Provision,
                Status = ProvisioningStatus.Pending
            };
            
            DocumentSession.Store(database);
            targetServer.DatabaseIds.Add(database.Id);

            DocumentSession.SaveChanges();

            return StatusCode(StatusCodes.Status202Accepted, new
            {
                Id = database.Id,
                Name = database.Name,
                Message = $"Database '{database.Name}' queued for creation on server '{targetServer.Name}'."
            });
        }

        /// <summary>
        ///     Delete a tenant's database.
        /// </summary>
        /// <param name="tenantId">
        ///     The tenant Id.
        /// </param>
        /// <param name="databaseId">
        ///     The database Id.
        /// </param>
        [HttpDelete("{tenantId}/databases/{databaseId}")]
        public IActionResult DeleteDatabase(string tenantId, string databaseId)
        {
            Tenant ownerTenant = DocumentSession.GetTenantById(tenantId);
            if (ownerTenant == null)
            {
                return NotFound(new
                {
                    Id = tenantId,
                    EntityType = "Tenant",
                    Message = $"No tenant found with Id {tenantId}"
                });
            }

            DatabaseServer targetServer = DocumentSession.GetDatabaseServerByTenantId(tenantId);
            if (targetServer == null)
            {
                return NotFound(new
                {
                    Id = tenantId,
                    EntityType = "DatabaseServer",
                    Message = $"No database server found for tenant with Id {tenantId}"
                });
            }

            DatabaseInstance targetDatabase = DocumentSession.GetDatabaseById(databaseId);
            if (targetDatabase == null)
            {
                return NotFound(new
                {
                    Id = databaseId,
                    EntityType = "Database",
                    Message = $"No database server found with Id {databaseId}"
                });
            }

            if (targetServer.Status != ProvisioningStatus.Ready)
            {
                return StatusCode(StatusCodes.Status409Conflict, new
                {
                    Id = targetServer.Id,
                    TenantId = tenantId,
                    EntityType = "DatabaseServer",
                    Action = targetServer.Action,
                    Status = targetServer.Status,
                    Message = $"Cannot delete database {targetDatabase.Name} in server {targetServer.Id} because a server-level action is already in progress."
                });
            }

            if (targetDatabase.Status != ProvisioningStatus.Ready)
            {
                return StatusCode(StatusCodes.Status409Conflict, new
                {
                    Id = targetServer.Id,
                    TenantId = tenantId,
                    EntityType = "Database",
                    Action = targetDatabase.Action,
                    Status = targetDatabase.Status,
                    Message = $"Cannot delete database {targetDatabase.Name} in server {targetServer.Id} because a database-level action is already in progress."
                });
            }

            targetDatabase.Action = ProvisioningAction.Deprovision;
            targetDatabase.Status = ProvisioningStatus.Pending;
            DocumentSession.SaveChanges();

            return StatusCode(StatusCodes.Status202Accepted, new
            {
                Id = targetDatabase.Id,
                Name = targetDatabase.Name,
                EntityType = "Database",
                Message = $"Database '{targetDatabase.Name}' queued for deletion on server '{targetServer.Name}'."
            });
        }
    }
}
