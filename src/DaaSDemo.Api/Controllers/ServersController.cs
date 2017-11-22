using HTTPlease;
using HTTPlease.Formatters;
using HTTPlease.Formatters.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Raven.Client.Documents.Session;
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
        /// <param name="documentSession">
        ///     The RavenDB document session for the current request.
        /// </param>
        /// <param name="logger">
        ///     The controller's log facility.
        /// </param>
        public ServersController(IDocumentSession documentSession, ILogger<ServersController> logger)
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
        ///     Get all servers.
        /// </summary>
        [HttpGet]
        public IActionResult List()
        {
            return Json(
                DocumentSession.Query<DatabaseServer>()
                    .Select(server => new DatabaseServerDetail(
                        server.Id,
                        server.Name,
                        server.Kind,
                        server.PublicFQDN,
                        server.PublicPort,
                        server.Action,
                        server.Phase,
                        server.Status,
                        server.TenantId,
                        server.TenantName
                    ))
            );
        }

        /// <summary>
        ///     Get a server by Id.
        /// </summary>
        /// <param name="serverId">
        ///     The server Id.
        /// </param>
        [HttpGet("{serverId}")]
        public IActionResult GetById(string serverId)
        {
            DatabaseServer server = DocumentSession.Load<DatabaseServer>(serverId);
            if (server != null)
            {
                return Json(new DatabaseServerDetail(
                    server.Id,
                    server.Name,
                    server.Kind,
                    server.PublicFQDN,
                    server.PublicPort,
                    server.Action,
                    server.Phase,
                    server.Status,
                    server.TenantId,
                    server.TenantName
                ));
            }

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
        [HttpGet("{serverId}/database")]
        public IActionResult GetDatabasesByServer(string serverId)
        {
            DatabaseServer server = DocumentSession.Load<DatabaseServer>(serverId);
            if (server == null)
            {
                return NotFound(new
                {
                    Id = serverId,
                    EntityType = "Server",
                    Message = $"No server found with Id {serverId}"
                });
            }

            return Json(
                DocumentSession.Query<DatabaseInstance>()
                    .Where(database => database.ServerId == serverId)
                    .Select(database => new DatabaseInstanceDetail(
                        database.Id,
                        database.Name,
                        database.DatabaseUser,
                        database.Action,
                        database.Status,
                        database.ServerId,
                        server.Name,
                        server.PublicFQDN,
                        server.PublicPort,
                        server.TenantId,
                        server.TenantName
                    ))
            );
        }
    }
}
