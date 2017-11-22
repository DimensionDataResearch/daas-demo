using HTTPlease;
using HTTPlease.Formatters;
using HTTPlease.Formatters.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

using StatusCodes = Microsoft.AspNetCore.Http.StatusCodes;

namespace DaaSDemo.Api.Controllers
{
    using System.Collections.Generic;
    using Data;
    using Models.Api;
    using Models.Data;
    using Raven.Client.Documents.Session;

    /// <summary>
    ///     Controller for the databases API.
    /// </summary>
    [Route("api/v1/databases")]
    public class DatabasesController
        : Controller
    {
        /// <summary>
        ///     Create a new databases API controller.
        /// </summary>
        /// <param name="documentSession">
        ///     The RavenDB document session for the current request.
        /// </param>
        /// <param name="logger">
        ///     The controller's log facility.
        /// </param>
        public DatabasesController(IDocumentSession documentSession, ILogger<DatabasesController> logger)
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
        ///     Get a database by Id.
        /// </summary>
        /// <param name="databaseId">
        ///     The database Id.
        /// </param>
        [HttpGet("{databaseId}")]
        public IActionResult GetById(string databaseId)
        {
            DatabaseInstance database = DocumentSession.Include<DatabaseInstance>(db => db.ServerId).Load(databaseId);
            if (database == null)
            {
                return NotFound(new
                {
                    Id = databaseId,
                    EntityType = "Database",
                    Message = $"Database not found with Id '{databaseId}'."
                });
            }

            DatabaseServer server = DocumentSession.Load<DatabaseServer>(database.ServerId);
            if (server == null)
            {
                return NotFound(new
                {
                    Id = database.ServerId,
                    EntityType = "Server",
                    Message = $"Database's server not found with Id '{database.ServerId}'."
                });
            }

            return Json(new DatabaseInstanceDetail(
                database.Id,
                database.Name,
                database.DatabaseUser,
                database.Action,
                database.Status,
                server.Id,
                server.Name,
                server.PublicFQDN,
                server.PublicPort,
                server.TenantId,
                server.TenantName
            ));
        }

        /// <summary>
        ///     Get all databases.
        /// </summary>
        [HttpGet]
        public IActionResult List()
        {
            // TODO: This method is now seriously inefficient - consider restructuring the data model or simplifying the output of operation to avoid joins.

            Dictionary<string, Tenant> tenants = DocumentSession.Query<Tenant>()
                .ToDictionary(
                    tenant => tenant.Id
                );

            Dictionary<string, DatabaseServer> servers = DocumentSession.Query<DatabaseServer>()
                .ToDictionary(
                    server => server.Id
                );

            DatabaseInstance[] databases = DocumentSession.Query<DatabaseInstance>()
                .ToArray();

            List<DatabaseInstanceDetail> databaseDetails = new List<DatabaseInstanceDetail>();
            foreach (DatabaseInstance database in databases)
            {
                Tenant tenant;
                if (!tenants.TryGetValue(database.TenantId, out tenant))
                    continue;

                DatabaseServer server;
                if (!servers.TryGetValue(database.ServerId, out server))
                    continue;

                databaseDetails.Add(new DatabaseInstanceDetail(
                    database.Id,
                    database.Name,
                    database.DatabaseUser,
                    database.Action,
                    database.Status,
                    server.Id,
                    server.Name,
                    server.PublicFQDN,
                    server.PublicPort,
                    tenant.Id,
                    tenant.Name
                ));
            }

            return Json(databaseDetails);
        }
    }
}
