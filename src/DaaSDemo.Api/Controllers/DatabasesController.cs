using HTTPlease;
using HTTPlease.Formatters;
using HTTPlease.Formatters.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
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
            DatabaseInstance database = DocumentSession
                .Include<DatabaseInstance>(db => db.TenantId)
                .Include<DatabaseInstance>(db => db.ServerId)
                .Load<DatabaseInstance>(databaseId);
            if (database == null)
            {
                return NotFound(new
                {
                    Id = databaseId,
                    EntityType = "Database",
                    Message = $"Database not found with Id '{databaseId}'."
                });
            }

            Tenant tenant = DocumentSession.Load<Tenant>(database.TenantId);
            if (tenant == null)
            {
                return NotFound(new
                {
                    Id = database.ServerId,
                    EntityType = "Tenant",
                    Message = $"Database {databaseId}'s tenant not found with Id '{database.TenantId}'."
                });
            }

            DatabaseServer server = DocumentSession.Load<DatabaseServer>(database.ServerId);
            if (server == null)
            {
                return NotFound(new
                {
                    Id = database.ServerId,
                    EntityType = "DatabaseServer",
                    Message = $"Database {databaseId}'s server not found with Id '{database.ServerId}'."
                });
            }

            return Json(
                new DatabaseInstanceDetail(database, server, tenant)
            );
        }

        /// <summary>
        ///     Get all databases.
        /// </summary>
        [HttpGet]
        public IActionResult List()
        {
            return Json(
                DocumentSession.Query<DatabaseInstanceDetail, DatabaseInstanceDetails>()
            );
        }
    }
}
