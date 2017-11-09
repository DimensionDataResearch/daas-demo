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
    using Data.Models;
    using Models.Api;

    /// <summary>
    ///     Controller for the tenants API.
    /// </summary>
    [Route("tenants")]
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
            Tenant matchingTenant = Entities.Tenants.FirstOrDefault(tenant => tenant.Id == tenantId);
            if (matchingTenant != null)
                return Json(matchingTenant);

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
            Tenant[] tenants = Entities.Tenants.OrderBy(tenant => tenant.Name).ToArray();

            return Json(tenants);
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
            Entities.Tenants.Add(tenant);
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
            DatabaseServer databaseServer = Entities.DatabaseServers.FirstOrDefault(server => server.TenantId == tenantId);
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
            DatabaseServer existingServer = Entities.DatabaseServers.FirstOrDefault(server => server.TenantId == tenantId);
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

            Tenant ownerTenant = Entities.Tenants.FirstOrDefault(tenant => tenant.Id == tenantId);
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

            var databaseServer = new DatabaseServer
            {
                Name = newDatabaseServer.Name,
                AdminPassword = newDatabaseServer.AdminPassword,
                TenantId = tenantId,
                Action = ProvisioningAction.Provision,
                Status = ProvisioningStatus.Pending
            };
            Entities.DatabaseServers.Add(databaseServer);
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
            DatabaseServer targetServer = Entities.DatabaseServers.FirstOrDefault(
                server => server.TenantId == tenantId
            );

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

            if (Entities.DatabaseInstances.Any(database => database.DatabaseServerId == targetServer.Id))
            {
                return StatusCode(StatusCodes.Status400BadRequest, new
                {
                    Id = targetServer.Id,
                    TenantId = tenantId,
                    EntityType = "DatabaseServer",
                    RequestedAction = ProvisioningAction.Reconfigure,
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
                    RequestedAction = ProvisioningAction.Reconfigure,
                    Action = targetServer.Action,
                    Status = targetServer.Status,
                    Message = $"Cannot de-provision database server {targetServer.Id} because an action is already in progress for this server."
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
            DatabaseServer targetServer = Entities.DatabaseServers.FirstOrDefault(
                server => server.TenantId == tenantId
            );

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

            if (Entities.DatabaseInstances.Any(database => database.DatabaseServerId == targetServer.Id))
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
            DatabaseServer databaseServer = Entities.DatabaseServers.FirstOrDefault(server => server.TenantId == tenantId);
            if (databaseServer == null)
            {
                return NotFound(new
                {
                    Id = tenantId,
                    EntityType = "DatabaseServer",
                    Message = $"No database server found for tenant with Id {tenantId}"
                });
            }

            Tenant ownerTenant = Entities.Tenants.FirstOrDefault(tenant => tenant.Id == tenantId);
            if (ownerTenant == null)
            {
                return NotFound(new
                {
                    Id = tenantId,
                    EntityType = "Tenant",
                    Message = $"No tenant found with Id {tenantId}"
                });
            }

            DatabaseInstanceDetail[] databases = 
                Entities.DatabaseInstances.Where(
                    database => database.DatabaseServerId == databaseServer.Id
                )
                .AsEnumerable()
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
            Tenant ownerTenant = Entities.Tenants.FirstOrDefault(tenant => tenant.Id == tenantId);
            if (ownerTenant == null)
            {
                return NotFound(new
                {
                    Id = tenantId,
                    EntityType = "Tenant",
                    Message = $"No tenant found with Id {tenantId}"
                });
            }

            DatabaseServer targetServer = Entities.DatabaseServers.FirstOrDefault(server => server.TenantId == tenantId);
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
                    Message = $"Cannot create a database in server {targetServer.Id} because an action is already in progress for this server."
                });
            }

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            DatabaseInstance existingDatabase = Entities.DatabaseInstances.FirstOrDefault(
                databaseInstance => databaseInstance.DatabaseServerId == targetServer.Id && databaseInstance.Name == newDatabase.Name
            );
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
                DatabaseServerId = targetServer.Id,
                Action = ProvisioningAction.Provision
            };

            Entities.DatabaseInstances.Add(database);
            Entities.SaveChanges();

            return StatusCode(StatusCodes.Status202Accepted, new
            {
                Id = database.Id,
                Name = database.Name,
                Message = $"Database '{database.Name}' queued for creation on server '{targetServer.Name}'."
            });
        }
    }
}
