using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Raven.Client.Documents;
using Raven.Client.Documents.Session;
using System;
using System.Threading.Tasks;

namespace DaaSDemo.Api.Controllers.Admin
{
    using Data.Indexes;
    using Models.Api;
    using Models.Data;

    /// <summary>
    ///     Controller for the users API.
    /// </summary>
    [Route("api/v1/admin/users")]
    public class UsersController
        : Controller
    {
        /// <summary>
        ///     Create a new users API controller.
        /// </summary>
        /// <param name="documentSession">
        ///     The RavenDB document session for the current request.
        /// </param>
        /// <param name="logger">
        ///     The controller's log facility.
        /// </param>
        public UsersController(IAsyncDocumentSession documentSession, ILogger<UsersController> logger)
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
        IAsyncDocumentSession DocumentSession { get; }

        /// <summary>
        ///     The controller's log facility.
        /// </summary>
        ILogger Log { get; }

        /// <summary>
        ///     Get all users.
        /// </summary>
        [HttpGet("")]
        public async Task<IActionResult> List()
        {
            return Json(
                await DocumentSession.Query<AppUser, AppUserDetails>()
                    .OrderBy(user => user.UserName)
                    .ProjectFromIndexFieldsInto<AppUserDetail>()
                    .ToListAsync()
            );
        }

        /// <summary>
        ///     Get a user by Id.
        /// </summary>
        /// <param name="userId">
        ///     The user's Id.
        /// </param>
        [HttpGet("{userId}")]
        public async Task<IActionResult> GetById(string userId)
        {
            AppUser user = await DocumentSession.LoadAsync<AppUser>(userId);
            if (user == null)
            {
                return NotFound(new
                {
                    Id = userId,
                    EntityType = "User",
                    Message = $"User not found with Id '{userId}'."
                });
            }

            return Ok(
                AppUserDetail.From(user)
            );
        }
    }
}

