using HTTPlease;
using HTTPlease.Formatters;
using HTTPlease.Formatters.Json;
using IdentityModel;
using IdentityServer4.AccessTokenValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Raven.Client.Documents;
using Raven.Client.Documents.Session;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

using StatusCodes = Microsoft.AspNetCore.Http.StatusCodes;

namespace DaaSDemo.Api.Controllers
{
    using Data;
    using Data.Indexes;
    using Models.Api;
    using Models.Data;

    /// <summary>
    ///     Controller for the user admin API.
    /// </summary>
    [Route("api/v1/admin/users")]
    public class UsersController
        : Controller
    {
        /// <summary>
        ///     Create a new users API controller.
        /// </summary>
        /// <param name="userManager">
        ///     The user-management facility.
        /// </param>
        /// <param name="documentSession">
        ///     The RavenDB document session for the current request.
        /// </param>
        /// <param name="logger">
        ///     The controller's log facility.
        /// </param>
        public UsersController(UserManager<AppUser> userManager, IAsyncDocumentSession documentSession, ILogger<UsersController> logger)
        {
            if (userManager == null)
                throw new ArgumentNullException(nameof(userManager));

            if (logger == null)
                throw new ArgumentNullException(nameof(logger));

            UserManager = userManager;
            Log = logger;
        }

        /// <summary>
        ///     The RavenDB document session for the current request.
        /// </summary>
        IAsyncDocumentSession DocumentSession { get; }

        /// <summary>
        ///     The user-management facility.
        /// </summary>
        UserManager<AppUser> UserManager { get; }

        /// <summary>
        ///     The controller's log facility.
        /// </summary>
        ILogger Log { get; }

        /// <summary>
        ///     List all DaaS application users.
        /// </summary>
        [HttpGet("")]
        public async Task<IActionResult> List()
        {
            return Ok(
                await DocumentSession.Query<AppUser>()
                    .ToListAsync()
            );
        }

        /// <summary>
        ///     Get information about the current (authenticated) user.
        /// </summary>
        [HttpGet("whoami"), Authorize("User")]
        public IActionResult WhoAmI()
        {
            return Ok(new
            {
                Name = User.Identity.Name,
                IsUser = User.IsInRole("User"),
                IsAdministrator = User.IsInRole("Administrator"),
                Roles = User.Claims
                    .Where(claim => claim.Type == JwtClaimTypes.Role)
                    .Select(claim => claim.Value)
            });
        }
    }
}
