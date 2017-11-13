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
    ///     Controller for the servers API.
    /// </summary>
    [Route("api/v1/servers")]
    public class ServersController
        : Controller
    {
        /// <summary>
        ///     Create a new servers API controller.
        /// </summary>
        /// <param name="entities">
        ///     The DaaS entity context.
        /// </param>
        /// <param name="logger">
        ///     The controller's log facility.
        /// </param>
        public ServersController(Entities entities, ILogger<ServersController> logger)
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
        ///     Get all servers.
        /// </summary>
        [HttpGet]
        public IActionResult List()
        {
            return Json(
                Entities.DatabaseServers
                    .Select(server => new DatabaseServerDetail(
                        server.Id,
                        server.Name,
                        server.PublicFQDN,
                        server.PublicPort,
                        server.Action,
                        server.Phase,
                        server.Status,
                        server.Tenant.Id,
                        server.Tenant.Name
                    ))
            );
        }

        /// <summary>
        ///     Get a server by Id.
        /// </summary>
        /// <param name="serverId">
        ///     The server Id.
        /// </param>
        [HttpGet("{serverId:int}")]
        public IActionResult GetById(int serverId)
        {
            DatabaseServerDetail serverDetail =
                Entities.DatabaseServers
                    .Where(server => server.Id == serverId)
                    .Select(server => new DatabaseServerDetail(
                        server.Id,
                        server.Name,
                        server.PublicFQDN,
                        server.PublicPort,
                        server.Action,
                        server.Phase,
                        server.Status,
                        server.Tenant.Id,
                        server.Tenant.Name
                    ))
                    .FirstOrDefault();

            if (serverDetail != null)
                return Json(serverDetail);

            return NotFound(new
            {
                Id = serverId,
                EntityType = "Server",
                Message = $"No server found with Id {serverId}"
            });
        }

        /// <summary>
        ///     Get all servers hosted by the specified server.
        /// </summary>
        /// <param name="serverId">
        ///     The server Id.
        /// </param>
        [HttpGet("{serverId:int}/database")]
        public IActionResult GetDatabasesByServer(int serverId)
        {
            return Json(
                Entities.DatabaseInstances
                    .Where(database => database.DatabaseServerId == serverId)
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
