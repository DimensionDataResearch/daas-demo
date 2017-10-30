using HTTPlease;
using HTTPlease.Formatters;
using HTTPlease.Formatters.Json;
using KubeNET.Swagger.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
    using Models;

    /// <summary>
    ///     Controller for the tenants API.
    /// </summary>
    [Route("tenants")]
    public class TenantsController
        : Controller
    {
        /// <summary>
        ///     The DaaS entity context.
        /// </summary>
        Entities _entities;

        /// <summary>
        ///     Create a new tenants API controller.
        /// </summary>
        /// <param name="entities">
        ///     The DaaS entity context.
        /// </param>
        public TenantsController(Entities entities)
        {
            if (entities == null)
                throw new ArgumentNullException(nameof(entities));
            
            _entities = entities;
        }

        /// <summary>
        ///     Get a tenant by Id.
        /// </summary>
        /// <param name="tenantId">
        ///     The tenant Id.
        /// </param>
        [HttpGet("{tenantId:int}")]
        public IActionResult GetById(int tenantId)
        {
            Tenant matchingTenant = _entities.Tenants.FirstOrDefault(tenant => tenant.Id == tenantId);
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
            Tenant[] tenants = _entities.Tenants.OrderBy(tenant => tenant.Name).ToArray();

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
            _entities.Tenants.Add(tenant);
            _entities.SaveChanges();

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
            DatabaseServer databaseServer = _entities.DatabaseServers.FirstOrDefault(server => server.TenantId == tenantId);
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
            DatabaseServer existingServer = _entities.DatabaseServers.FirstOrDefault(server => server.TenantId == tenantId);
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

            Tenant ownerTenant = _entities.Tenants.FirstOrDefault(tenant => tenant.Id == tenantId);
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
            _entities.DatabaseServers.Add(databaseServer);
            _entities.SaveChanges();

            return StatusCode(StatusCodes.Status202Accepted, new
            {
                Id = databaseServer.Id,
                Name = databaseServer.Name,
                Message = $"Database server {databaseServer.Id} queued for creation."
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
            DatabaseServer targetServer = _entities.DatabaseServers.FirstOrDefault(
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

            if (_entities.DatabaseInstances.Any(database => database.DatabaseServerId == targetServer.Id))
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
            _entities.SaveChanges();

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
            DatabaseServer databaseServer = _entities.DatabaseServers.FirstOrDefault(server => server.TenantId == tenantId);
            if (databaseServer == null)
            {
                return NotFound(new
                {
                    Id = tenantId,
                    EntityType = "DatabaseServer",
                    Message = $"No database server found for tenant with Id {tenantId}"
                });
            }

            Tenant ownerTenant = _entities.Tenants.FirstOrDefault(tenant => tenant.Id == tenantId);
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
                _entities.DatabaseInstances.Where(
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
            Tenant ownerTenant = _entities.Tenants.FirstOrDefault(tenant => tenant.Id == tenantId);
            if (ownerTenant == null)
            {
                return NotFound(new
                {
                    Id = tenantId,
                    EntityType = "Tenant",
                    Message = $"No tenant found with Id {tenantId}"
                });
            }

            DatabaseServer databaseServer = _entities.DatabaseServers.FirstOrDefault(server => server.TenantId == tenantId);
            if (databaseServer == null)
            {
                return NotFound(new
                {
                    Id = tenantId,
                    EntityType = "DatabaseServer",
                    Message = $"No database server found for tenant with Id {tenantId}"
                });
            }

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            DatabaseInstance existingDatabase = _entities.DatabaseInstances.FirstOrDefault(
                databaseInstance => databaseInstance.DatabaseServerId == databaseServer.Id && databaseInstance.Name == newDatabase.Name
            );
            if (existingDatabase != null)
            {
                return StatusCode(StatusCodes.Status409Conflict, new
                {
                    Id = existingDatabase.Id,
                    Name = existingDatabase.Name,
                    EntityType = "Database",
                    Message = $"Database '{existingDatabase.Name}' already exists on server '{databaseServer.Name}'."
                });
            }

            var database = new DatabaseInstance
            {
                Name = newDatabase.Name,
                DatabaseUser = newDatabase.DatabaseUser,
                DatabasePassword = newDatabase.DatabasePassword,
                DatabaseServerId = databaseServer.Id,
                Action = ProvisioningAction.Provision
            };

            _entities.DatabaseInstances.Add(database);
            _entities.SaveChanges();

            return StatusCode(StatusCodes.Status202Accepted, new
            {
                Id = database.Id,
                Name = database.Name,
                Message = $"Database '{database.Name}' queued for creation on server '{databaseServer.Name}'."
            });
        }
    }
}
