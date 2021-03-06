using HTTPlease;
using HTTPlease.Formatters;
using HTTPlease.Formatters.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Raven.Client.Documents;
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
    using Raven.Client.Documents.Linq;

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
        ///     Get all servers owned by a tenant.
        /// </summary>
        /// <param name="tenantId">
        ///     The tenant Id.
        /// </param>
        /// <param name="ensureUpToDate">
        ///     Ensure that the results are as up-to-date as possible?
        /// </param>
        [HttpGet("{tenantId}/servers")]
        public IActionResult GetServers(string tenantId, bool ensureUpToDate = false)
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

            IRavenQueryable<DatabaseServer> query = DocumentSession.Query<DatabaseServer, DatabaseServerDetails>();
            if (ensureUpToDate)
            {
                query = query.Customize(
                    queryConfig => queryConfig.WaitForNonStaleResults(
                        waitTimeout: TimeSpan.FromSeconds(5)
                    )
                );
            }

            return Json(
                query.Where(
                    server => server.TenantId == tenantId
                )
                .OrderBy(server => server.Name)
                .ProjectFromIndexFieldsInto<DatabaseServerDetail>()
            );
        }

        /// <summary>
        ///     Get all databases owned by a tenant.
        /// </summary>
        /// <param name="tenantId">
        ///     The tenant Id.
        /// </param>
        /// <param name="ensureUpToDate">
        ///     Ensure that the results are as up-to-date as possible?
        /// </param>
        [HttpGet("{tenantId}/databases")]
        public IActionResult GetDatabases(string tenantId, bool ensureUpToDate = false)
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

            IRavenQueryable<DatabaseInstance> query = DocumentSession.Query<DatabaseInstance, DatabaseInstanceDetails>();
            if (ensureUpToDate)
            {
                query = query.Customize(
                    queryConfig => queryConfig.WaitForNonStaleResults(
                        waitTimeout: TimeSpan.FromSeconds(5)
                    )
                );
            }

            return Json(
                query.Where(
                    database => database.TenantId == tenantId
                )
                .ProjectFromIndexFieldsInto<DatabaseInstanceDetail>()
            );
        }
    }
}
