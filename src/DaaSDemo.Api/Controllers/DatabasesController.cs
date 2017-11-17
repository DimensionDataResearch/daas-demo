using HTTPlease;
using HTTPlease.Formatters;
using HTTPlease.Formatters.Json;
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
    ///     Controller for the databases API.
    /// </summary>
    [Route("api/v1/databases")]
    public class DatabasesController
        : Controller
    {
        /// <summary>
        ///     Create a new databases API controller.
        /// </summary>
        /// <param name="entities">
        ///     The DaaS entity context.
        /// </param>
        /// <param name="logger">
        ///     The controller's log facility.
        /// </param>
        public DatabasesController(Entities entities, ILogger<DatabasesController> logger)
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
        ///     Get a database by Id.
        /// </summary>
        /// <param name="databaseId">
        ///     The database Id.
        /// </param>
        [HttpGet("{databaseId:int}")]
        public IActionResult GetById(int databaseId)
        {
            DatabaseInstanceDetail databaseDetail = Entities.DatabaseInstances
                .Where(database => database.Id == databaseId)
                .Select(database => new DatabaseInstanceDetail(
                    database.Id,
                    database.Name,
                    database.DatabaseUser,
                    database.Action,
                    database.Status,
                    database.DatabaseServer.Id,
                    database.DatabaseServer.Name,
                    database.DatabaseServer.PublicFQDN,
                    database.DatabaseServer.PublicPort,
                    database.DatabaseServer.Tenant.Id,
                    database.DatabaseServer.Tenant.Name
                ))
                .FirstOrDefault();

            if (databaseDetail != null)
                return Json(databaseDetail);

            return NotFound(new
            {
                Id = databaseId,
                EntityType = "Database",
                Message = $"No database found with Id {databaseId}"
            });
        }

        /// <summary>
        ///     Get all databases.
        /// </summary>
        [HttpGet]
        public IActionResult List()
        {
            return Json(
                Entities.DatabaseInstances
                    .Select(database => new DatabaseInstanceDetail(
                        database.Id,
                        database.Name,
                        database.DatabaseUser,
                        database.Action,
                        database.Status,
                        database.DatabaseServer.Id,
                        database.DatabaseServer.Name,
                        database.DatabaseServer.PublicFQDN,
                        database.DatabaseServer.PublicPort,
                        database.DatabaseServer.Tenant.Id,
                        database.DatabaseServer.Tenant.Name
                    ))
            );
        }
    }
}
