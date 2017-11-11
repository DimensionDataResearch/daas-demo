using HTTPlease;
using HTTPlease.Formatters;
using HTTPlease.Formatters.Json;
using KubeNET.Swagger.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

using StatusCodes = Microsoft.AspNetCore.Http.StatusCodes;

namespace DaaSDemo.Api.Controllers
{
    using Data;
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
        /// <param name="entities">
        ///     The DaaS entity context.
        /// </param>
        /// <param name="logger">
        ///     The controller's log facility.
        /// </param>
        public TenantsController(Entities entities, ILogger<TenantsController> logger)
        {
            if (entities == null)
                throw new ArgumentNullException(nameof(entities));

            if (logger == null)
                throw new ArgumentNullException(nameof(logger));
            
            Entities = entities;
            Log = logger;
        }

        /// <summary>
        ///     The DaaS entity context.
        /// </summary>
        Entities Entities { get; }

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
        [HttpGet("{tenantId:int}")]
        public IActionResult GetById(int tenantId)
        {
            Tenant tenant = Entities.GetTenantById(tenantId);
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
                Entities.GetAllTenants()
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

            var tenant = Entities.AddTenant(
                name: newTenant.Name
            );
            Entities.SaveChanges();

            return Json(tenant);
        }

        /// <summary>
        ///     Get a tenant's database server.
        /// </summary>
        /// <param name="tenantId">
        ///     The tenant Id.
        /// </param>
        [HttpGet("{tenantId:int}/server")]
        public IActionResult GetServer(int tenantId)
        {
            DatabaseServer databaseServer = Entities.GetDatabaseServerByTenantId(tenantId);
            if (databaseServer == null)
            {
                return NotFound(new
                {
                    Id = tenantId,
                    EntityType = "DatabaseServer",
                    Message = $"No database server found for tenant with Id {tenantId}"
                });
            }

            return Json(databaseServer);
        }

        /// <summary>
        ///     Provision a database server for a tenant.
        /// </summary>
        /// <param name="tenantId">
        ///     The tenant Id.
        /// </param>
        /// <param name="newDatabaseServer">
        ///     The request body as a <see cref="NewDatabaseServer"/>.
        /// </param>
        [HttpPost("{tenantId:int}/server")]
        public IActionResult CreateServer(int tenantId, [FromBody] NewDatabaseServer newDatabaseServer)
        {
            DatabaseServer existingServer = Entities.GetDatabaseServerByTenantId(tenantId);
            if (existingServer != null)
            {
                return StatusCode(StatusCodes.Status409Conflict, new
                {
                    Id = tenantId,
                    EntityType = "DatabaseServer",
                    Message = $"Database server already exists for tenant with Id {tenantId}",
                    ExistingServer = existingServer
                });
            }

            Tenant ownerTenant = Entities.GetTenantById(tenantId);
            if (ownerTenant == null)
            {
                return NotFound(new
                {
                    Id = tenantId,
                    EntityType = "Tenant",
                    Message = $"No tenant found with Id {tenantId}."
                });
            }

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var databaseServer = Entities.AddDatabaseServer(
                tenantId: tenantId,
                name: newDatabaseServer.Name,
                adminPassword: newDatabaseServer.AdminPassword,
                action: ProvisioningAction.Provision
            );
            Entities.SaveChanges();

            return StatusCode(StatusCodes.Status202Accepted, new
            {
                Id = databaseServer.Id,
                Name = databaseServer.Name,
                Message = $"Database server {databaseServer.Id} queued for creation."
            });
        }

        /// <summary>
        ///     Reconfigure / repair a tenant's database server.
        /// </summary>
        /// <param name="tenantId">
        ///     The tenant Id.
        /// </param>
        [HttpPost("{tenantId:int}/server/reconfigure")]
        public IActionResult ReconfigureServer(int tenantId)
        {
            DatabaseServer targetServer = Entities.GetDatabaseServerByTenantId(tenantId);
            if (targetServer == null)
            {
                return NotFound(new
                {
                    Id = (string)null,
                    TenantId = tenantId,
                    EntityType = "DatabaseServer",
                    Message = $"No database server found for tenant {tenantId}."
                });
            }

            if (targetServer.Action != ProvisioningAction.None)
            {
                return StatusCode(StatusCodes.Status409Conflict, new
                {
                    Id = targetServer.Id,
                    TenantId = tenantId,
                    EntityType = "DatabaseServer",
                    RequestedAction = ProvisioningAction.Reconfigure,
                    Action = targetServer.Action,
                    Status = targetServer.Status,
                    Message = $"Cannot reconfigure database server {targetServer.Id} because another server-level action is already in progress."
                });
            }

            targetServer.Action = ProvisioningAction.Reconfigure;
            Entities.SaveChanges();

            return StatusCode(StatusCodes.Status202Accepted, new
            {
                Id = tenantId,
                Message = $"Database server {targetServer.Id} queued for reconfiguration.",
                EntityType = "DatabaseServer"
            });
        }

        /// <summary>
        ///     Deprovision a tenant's database server.
        /// </summary>
        /// <param name="tenantId">
        ///     The tenant Id.
        /// </param>
        [HttpDelete("{tenantId:int}/server")]
        public IActionResult DestroyServer(int tenantId)
        {
            DatabaseServer targetServer = Entities.GetDatabaseServerByTenantId(tenantId);
            if (targetServer == null)
            {
                return NotFound(new
                {
                    Id = (string)null,
                    TenantId = tenantId,
                    EntityType = "DatabaseServer",
                    Message = $"No database server found for tenant {tenantId}."
                });
            }

            if (Entities.DoesServerHaveDatabases(targetServer.Id))
            {
                return StatusCode(StatusCodes.Status400BadRequest, new
                {
                    Id = targetServer.Id,
                    TenantId = tenantId,
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
                    TenantId = tenantId,
                    EntityType = "DatabaseServer",
                    RequestedAction = ProvisioningAction.Deprovision,
                    Action = targetServer.Action,
                    Status = targetServer.Status,
                    Message = $"Cannot de-provision database server {targetServer.Id} because an action is already in progress for this server."
                });
            }

            targetServer.Action = ProvisioningAction.Deprovision;
            Entities.SaveChanges();

            return StatusCode(StatusCodes.Status202Accepted, new
            {
                Id = tenantId,
                Message = $"Database server {targetServer.Id} queued for deletion.",
                EntityType = "DatabaseServer"
            });
        }

        /// <summary>
        ///     Get all databases owned by a tenant.
        /// </summary>
        /// <param name="tenantId">
        ///     The tenant Id.
        /// </param>
        [HttpGet("{tenantId:int}/databases")]
        public IActionResult GetDatabases(int tenantId)
        {
            DatabaseServer databaseServer = Entities.GetDatabaseServerByTenantId(tenantId);
            if (databaseServer == null)
            {
                return NotFound(new
                {
                    Id = tenantId,
                    EntityType = "DatabaseServer",
                    Message = $"No database server found for tenant with Id {tenantId}"
                });
            }

            Tenant ownerTenant = Entities.GetTenantById(tenantId);
            if (ownerTenant == null)
            {
                return NotFound(new
                {
                    Id = tenantId,
                    EntityType = "Tenant",
                    Message = $"No tenant found with Id {tenantId}"
                });
            }

            DatabaseInstanceDetail[] databases = Entities.GetDatabaseInstancesByServer(databaseServer.Id)
                .Select(database => new DatabaseInstanceDetail(database))
                .ToArray();
            
            return Json(databases);
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
        [HttpPost("{tenantId:int}/databases")]
        public IActionResult CreateDatabase(int tenantId, [FromBody] NewDatabaseInstance newDatabase)
        {
            Tenant ownerTenant = Entities.GetTenantById(tenantId);
            if (ownerTenant == null)
            {
                return NotFound(new
                {
                    Id = tenantId,
                    EntityType = "Tenant",
                    Message = $"No tenant found with Id {tenantId}"
                });
            }

            DatabaseServer targetServer = Entities.GetDatabaseServerByTenantId(tenantId);
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

            DatabaseInstance existingDatabase = Entities.GetDatabaseInstanceByName(newDatabase.Name, targetServer.Id);
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

            DatabaseInstance database = Entities.AddDatabaseInstance(
                name: newDatabase.Name,
                serverId: targetServer.Id,
                databaseUser: newDatabase.DatabaseUser,
                databasePassword: newDatabase.DatabasePassword,
                action: ProvisioningAction.Provision
            );

            Entities.SaveChanges();

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
        [HttpDelete("{tenantId:int}/databases/{databaseId}")]
        public IActionResult DeleteDatabase(int tenantId, int databaseId)
        {
            Tenant ownerTenant = Entities.GetTenantById(tenantId);
            if (ownerTenant == null)
            {
                return NotFound(new
                {
                    Id = tenantId,
                    EntityType = "Tenant",
                    Message = $"No tenant found with Id {tenantId}"
                });
            }

            DatabaseServer targetServer = Entities.GetDatabaseServerByTenantId(tenantId);
            if (targetServer == null)
            {
                return NotFound(new
                {
                    Id = tenantId,
                    EntityType = "DatabaseServer",
                    Message = $"No database server found for tenant with Id {tenantId}"
                });
            }

            DatabaseInstance targetDatabase = Entities.GetDatabaseInstanceById(databaseId);
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
            Entities.SaveChanges();

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
