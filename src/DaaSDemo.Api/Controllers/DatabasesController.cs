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
                DocumentSession.Query<DatabaseInstance, DatabaseInstanceDetails>()
                    .ProjectFromIndexFieldsInto<DatabaseInstanceDetail>()
            );
        }

        /// <summary>
        ///     Provision a database for a tenant.
        /// </summary>
        /// <param name="newDatabase">
        ///     The request body as a <see cref="NewDatabase"/>.
        /// </param>
        [HttpPost]
        public IActionResult CreateDatabase([FromBody] NewDatabaseInstance newDatabase)
        {
            if (newDatabase == null)
            {
                return BadRequest(new
                {
                    EntityType = "Database",
                    Message = $"Must supply database details in the request body."
                });
            }
            
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            DatabaseServer targetServer = DocumentSession.GetDatabaseServerById(newDatabase.ServerId);
            if (targetServer == null)
            {
                return NotFound(new
                {
                    Id = newDatabase.ServerId,
                    EntityType = "DatabaseServer",
                    Message = $"No database server found for tenant with Id {newDatabase.ServerId}"
                });
            }

            if (targetServer.Kind != DatabaseServerKind.SqlServer)
            {
                return BadRequest(new
                {
                    ServerId = targetServer.Id,
                    ServerKind = targetServer.Kind,
                    Reason = "NotImplemented",
                    Message = $"Creation of databases in ${targetServer.Kind} servers is not supported yet."
                });
            }

            if (targetServer.Status != ProvisioningStatus.Ready)
            {
                return StatusCode(StatusCodes.Status409Conflict, new
                {
                    Id = targetServer.Id,
                    EntityType = "DatabaseServer",
                    Action = targetServer.Action,
                    Status = targetServer.Status,
                    Message = $"Cannot create a database in server {targetServer.Id} because a server-level action is already in progress."
                });
            }

            Tenant ownerTenant = DocumentSession.GetTenantById(targetServer.TenantId);
            if (ownerTenant == null)
            {
                return NotFound(new
                {
                    Id = targetServer.TenantId,
                    EntityType = "Tenant",
                    Message = $"Target server's owning tenant not found with Id {targetServer.TenantId}."
                });
            }

            DatabaseInstance existingDatabase = DocumentSession.GetDatabaseInstanceByName(newDatabase.Name, targetServer.Id);
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
                Storage =
                {
                    SizeMB = newDatabase.SizeMB
                },
                ServerId = targetServer.Id,
                TenantId = ownerTenant.Id,
                Action = ProvisioningAction.Provision,
                Status = ProvisioningStatus.Pending
            };
            
            DocumentSession.Store(database);
            targetServer.DatabaseIds.Add(database.Id);

            DocumentSession.SaveChanges();

            return StatusCode(StatusCodes.Status202Accepted, new
            {
                Id = database.Id,
                Name = database.Name,
                Message = $"Database '{database.Name}' queued for creation on server '{targetServer.Name}'."
            });
        }

        /// <summary>
        ///     Delete a database.
        /// </summary>
        /// <param name="databaseId">
        ///     The database Id.
        /// </param>
        [HttpDelete("{databaseId}")]
        public IActionResult DeleteDatabase(string databaseId)
        {
            DatabaseInstance targetDatabase = DocumentSession.GetDatabaseById(databaseId);
            if (targetDatabase == null)
            {
                return NotFound(new
                {
                    Id = databaseId,
                    EntityType = "Database",
                    Message = $"No database found with Id '{databaseId}'."
                });
            }

            Tenant ownerTenant = DocumentSession.GetTenantById(targetDatabase.TenantId);
            if (ownerTenant == null)
            {
                return NotFound(new
                {
                    Id = targetDatabase.TenantId,
                    EntityType = "Tenant",
                    Message = $"Database's owning tenant not found with Id '{targetDatabase.TenantId}'."
                });
            }

            DatabaseServer targetServer = DocumentSession.GetDatabaseServerById(targetDatabase.ServerId);
            if (targetServer == null)
            {
                return NotFound(new
                {
                    Id = targetDatabase.ServerId,
                    EntityType = "DatabaseServer",
                    Message = $"Database server not found with Id '{targetDatabase.ServerId}'."
                });
            }

            if (targetServer.Status != ProvisioningStatus.Ready)
            {
                return StatusCode(StatusCodes.Status409Conflict, new
                {
                    Id = targetServer.Id,
                    TenantId = ownerTenant.Id,
                    EntityType = "DatabaseServer",
                    Action = targetServer.Action,
                    Status = targetServer.Status,
                    Message = $"Cannot delete database {targetDatabase.Name} in server {targetServer.Id} because a server-level action ({targetServer.Action}) is already in progress."
                });
            }

            if (targetDatabase.Status != ProvisioningStatus.Ready)
            {
                return StatusCode(StatusCodes.Status409Conflict, new
                {
                    Id = targetServer.Id,
                    ServerId = targetServer.Id,
                    TenantId = ownerTenant.Id,
                    EntityType = "Database",
                    Action = targetDatabase.Action,
                    Status = targetDatabase.Status,
                    Message = $"Cannot delete database {targetDatabase.Name} in server {targetServer.Id} because a database-level action ({targetDatabase.Action}) is already in progress."
                });
            }

            targetDatabase.Action = ProvisioningAction.Deprovision;
            targetDatabase.Status = ProvisioningStatus.Pending;
            DocumentSession.SaveChanges();

            return StatusCode(StatusCodes.Status202Accepted, new
            {
                Id = targetDatabase.Id,
                Name = targetDatabase.Name,
                EntityType = "Database",
                Message = $"Database '{targetDatabase.Name}' queued for deletion on server '{targetServer.Name}'."
            });
        }

        /// <summary>
        ///     Reconfigure / repair a database.
        /// </summary>
        /// <param name="databaseId">
        ///     The database Id.
        /// </param>
        [HttpPost("{databaseId}/reconfigure")]
        public IActionResult ReconfigureDatabase(string databaseId)
        {
            DatabaseInstance targetDatabase = DocumentSession.GetDatabaseById(databaseId);
            if (targetDatabase == null)
            {
                return NotFound(new
                {
                    Id = databaseId,
                    EntityType = "Database",
                    Message = $"No database found with Id '{databaseId}'."
                });
            }

            Tenant ownerTenant = DocumentSession.GetTenantById(targetDatabase.TenantId);
            if (ownerTenant == null)
            {
                return NotFound(new
                {
                    Id = targetDatabase.TenantId,
                    EntityType = "Tenant",
                    Message = $"Database's owning tenant not found with Id '{targetDatabase.TenantId}'."
                });
            }

            DatabaseServer targetServer = DocumentSession.GetDatabaseServerById(targetDatabase.ServerId);
            if (targetServer == null)
            {
                return NotFound(new
                {
                    Id = targetDatabase.ServerId,
                    EntityType = "DatabaseServer",
                    Message = $"Database server not found with Id '{targetDatabase.ServerId}'."
                });
            }

            if (targetServer.Status != ProvisioningStatus.Ready)
            {
                return StatusCode(StatusCodes.Status409Conflict, new
                {
                    Id = targetServer.Id,
                    TenantId = ownerTenant.Id,
                    EntityType = "DatabaseServer",
                    Action = targetServer.Action,
                    Status = targetServer.Status,
                    Message = $"Cannot re-configure database {targetDatabase.Name} in server {targetServer.Id} because a server-level action ({targetServer.Action}) is already in progress."
                });
            }

            if (targetDatabase.Status != ProvisioningStatus.Ready)
            {
                return StatusCode(StatusCodes.Status409Conflict, new
                {
                    Id = targetServer.Id,
                    ServerId = targetServer.Id,
                    TenantId = ownerTenant.Id,
                    EntityType = "Database",
                    Action = targetDatabase.Action,
                    Status = targetDatabase.Status,
                    Message = $"Cannot re-configure database {targetDatabase.Name} in server {targetServer.Id} because a database-level action ({targetDatabase.Action}) is already in progress."
                });
            }

            targetDatabase.Action = ProvisioningAction.Reconfigure;
            targetDatabase.Status = ProvisioningStatus.Pending;
            DocumentSession.SaveChanges();

            return StatusCode(StatusCodes.Status202Accepted, new
            {
                Id = targetDatabase.Id,
                Name = targetDatabase.Name,
                EntityType = "Database",
                Message = $"Database '{targetDatabase.Name}' queued for reconfiguration on server '{targetServer.Name}'."
            });
        }
    }
}
