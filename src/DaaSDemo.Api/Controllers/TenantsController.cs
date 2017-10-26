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
            Tenant[] tenants = _entities.Tenants.ToArray();

            return Json(tenants);
        }

        /// <summary>
        ///     Create a tenant.
        /// </summary>
        /// <param name="tenant">
        ///     The request body as a <see cref="Tenant"/>.
        /// </param>
        [HttpPost]
        public IActionResult Create([FromBody, Bind(nameof(Tenant.Name))] Tenant tenant)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var entry = _entities.Tenants.Add(tenant);
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
        /// <param name="databaseServer">
        ///     The request body as a <see cref="NewDatabaseServer"/>.
        /// </param>
        [HttpPost("{tenantId:int}/server")]
        public IActionResult CreateServer(int tenantId, [FromBody] NewDatabaseServer databaseServer)
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
                    Message = $"No tenant found with Id {tenantId}"
                });
            }

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var newDatabaseServer = new DatabaseServer
            {
                Name = databaseServer.Name,
                AdminPassword = databaseServer.AdminPassword,
                TenantId = tenantId,
                Action = ProvisioningAction.Provision,
                Status = ProvisioningStatus.Pending
            };
            _entities.DatabaseServers.Add(newDatabaseServer);
            _entities.SaveChanges();

            return Json(newDatabaseServer);
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
        /// <param name="database">
        ///     The request body as a <see cref="Database"/>.
        /// </param>
        [HttpPost("{tenantId:int}/databases")]
        public IActionResult CreateDatabase(int tenantId, [FromBody] DatabaseInstance database) // TODO: Define NewDatabaseInstance model.
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

            database.DatabaseServerId = databaseServer.Id;

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // TODO: Check for unique database name.

            _entities.DatabaseInstances.Add(database);
            _entities.SaveChanges();

            return Json(database);
        }
    }
}
