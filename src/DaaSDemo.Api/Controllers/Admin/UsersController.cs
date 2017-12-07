using IdentityModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Raven.Client.Documents;
using Raven.Client.Documents.Session;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace DaaSDemo.Api.Controllers.Admin
{
    using Data.Indexes;
    using Microsoft.AspNetCore.Http;
    using Models.Api;
    using Models.Data;

    /// <summary>
    ///     Controller for the users API.
    /// </summary>
    [Route("api/v1/admin/users"), Authorize("Administrator")]
    public class UsersController
        : Controller
    {
        /// <summary>
        ///     Create a new users API controller.
        /// </summary>
        /// <param name="userManager">
        ///     The ASP.NET Core Identity user manager.
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

            if (documentSession == null)
                throw new ArgumentNullException(nameof(documentSession));

            if (logger == null)
                throw new ArgumentNullException(nameof(logger));            
            
            UserManager = userManager;
            DocumentSession = documentSession;
            Log = logger;
        }

        /// <summary>
        ///     The ASP.NET Core Identity user manager.
        /// </summary>
        UserManager<AppUser> UserManager { get; }

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
                    .ProjectFromIndexFieldsInto<AppUserDetail>()
                    .OrderBy(user => user.Name)
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
                return UserNotFoundById(userId);

            return Ok(
                AppUserDetail.From(user)
            );
        }

        /// <summary>
        ///     Create a new user.
        /// </summary>
        /// <param name="newUser">
        ///     The request body as a <see cref="NewUser"/>.
        /// </param>
        [HttpPost("")]
        public async Task<IActionResult> Create([FromBody] NewUser newUser)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            AppUser user = new AppUser
            {
                DisplayName = newUser.Name,
                UserName = newUser.Email,
                Email = newUser.Email
            };

            IdentityResult createResult = await UserManager.CreateAsync(user);
            if (createResult != IdentityResult.Success)
            {
                Log.LogError("Failed to create user {UserName} ({UserEmail}). {@IdentityResult}",
                    newUser.Name,
                    newUser.Email,
                    createResult
                );
                
                IdentityError createError = createResult.Errors.First();

                return BadRequest(new
                {
                    EntityType = "User",
                    Reason = "Identity." + createError.Code,
                    Message = $"Failed to create user '{newUser.Name}'. " + createError.Description
                });
            }

            Log.LogInformation("Created user {UserName} ({UserEmail}) with Id {UserId}.",
                newUser.Name,
                newUser.Email,
                user.Id
            );

            IdentityResult addPasswordResult = await UserManager.AddPasswordAsync(user, newUser.Password);
            if (addPasswordResult != IdentityResult.Success)
            {
                Log.LogError("Failed to add password for user {UserName} ({UserEmail}). {@IdentityResult}",
                    newUser.Name,
                    newUser.Email,
                    addPasswordResult
                );

                IdentityError addPasswordError = createResult.Errors.First();

                return BadRequest(new
                {
                    Id = user.Id,
                    EntityType = "User",
                    Reason = "Identity." + addPasswordError.Code,
                    Message = $"Failed to set password for user '{newUser.Name}'. " + addPasswordError.Description
                });
            }

            if (newUser.IsAdmin)
            {
                IdentityResult makeAdminResult = await UserManager.AddToRoleAsync(user, "Administrator");
                if (makeAdminResult != IdentityResult.Success)
                {
                    Log.LogError("Failed to assign user {UserName} ({UserEmail}) to the {RoleName} role. {@IdentityResult}",
                        newUser.Name,
                        newUser.Email,
                        "Administrator",
                        makeAdminResult
                    );

                    IdentityError makeAdminError = createResult.Errors.First();

                    return BadRequest(new
                    {
                        Id = user.Id,
                        EntityType = "User",
                        Reason = "Identity." + makeAdminError.Code,
                        Message = $"Failed to assign user '{newUser.Name}' to the 'Administrator' role. " + makeAdminError.Description
                    });
                }
            }

            string userId = user.Id;
            user = await UserManager.FindByIdAsync(userId);
            if (user == null)
                throw new Exception($"Failed to retrieve newly-created user '{userId}' by Id."); // TODO: Custom exception type.

            // User account will be locked out until their email address is confirmed; so let's short-circuit the process.
            user.EmailConfirmed = true;
            user.LockoutEnabled = false;

            IdentityResult enableResult = await UserManager.UpdateAsync(user);
            if (enableResult != IdentityResult.Success)
            {
                Log.LogError("Failed to enable user {UserName} ({UserEmail}). {@IdentityResult}",
                    newUser.Name,
                    newUser.Email,
                    enableResult
                );

                IdentityError enableError = createResult.Errors.First();

                return BadRequest(new
                {
                    Id = user.Id,
                    EntityType = "User",
                    Reason = "Identity." + enableError.Code,
                    Message = $"Failed to set password for user '{newUser.Name}'. " + enableError.Description
                });
            }

            return Ok(
                AppUserDetail.From(user)
            );
        }

        /// <summary>
        ///     Delete a user.
        /// </summary>
        /// <param name="userId">
        ///     The Id of the user to delete.
        /// </param>
        [HttpDelete("{userId}")]
        public async Task<IActionResult> Delete(string userId)
        {
            AppUser user = await UserManager.FindByIdAsync(userId);
            if (user == null)
                return UserNotFoundById(userId);

            if (user.IsSuperUser)
            {
                return BadRequest(new
                {
                    Id = userId,
                    EntityType = "User",
                    Reason = "CannotDeleteSuperUser",
                    Message = "Cannot delete super-users."
                });
            }

            IdentityResult deleteResult = await UserManager.DeleteAsync(user);
            if (deleteResult != IdentityResult.Success)
            {
                Log.LogError("Failed to delete user {UserName} ({UserEmail}). {@IdentityResult}",
                    user.DisplayName,
                    user.Email,
                    deleteResult
                );

                IdentityError deleteError = deleteResult.Errors.First();

                return BadRequest(new
                {
                    Id = userId,
                    EntityType = "User",
                    Reason = "Identity." + deleteError.Code,
                    Message = $"Failed to delete user '{userId}'. " + deleteError.Description
                });
            }

            return Ok(new
            {
                Success = true,
                Id = userId,
                EntityType = "User",
                Message = $"Deleted user '{userId}'."
            });
        }

        /// <summary>
        ///     Get information about a user's role memberships.
        /// </summary>
        /// <param name="userId">
        ///     The target user Id.
        /// </param>
        [HttpGet("{userId}/roles")]
        public async Task<IActionResult> GetRoles(string userId)
        {
            AppUser user = await DocumentSession.LoadAsync<AppUser>(userId);
            if (user == null)
                return UserNotFoundById(userId);

            return Ok(user.Roles);
        }

        /// <summary>
        ///     Change the current user's password.
        /// </summary>
        /// <param name="setPassword">
        ///     The set-user-password model.
        /// </param>
        [HttpPost("{userId}/password"), Authorize("User")]
        public async Task<IActionResult> SetPassword(string userId, [FromBody] SetPassword setPassword)
        {
            if (String.Equals(setPassword.NewPassword, setPassword.NewPasswordConfirmation, StringComparison.OrdinalIgnoreCase))
                ModelState.AddModelError("NewPasswordConfirmation", "Passwords do not match.");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            AppUser user = await UserManager.FindByIdAsync(userId);
            if (user == null)
                return UserNotFoundById(userId);
            
            if (await UserManager.HasPasswordAsync(user))
            {
                IdentityResult removePasswordResult = await UserManager.RemovePasswordAsync(user);
                if (!removePasswordResult.Succeeded)
                {
                    Log.LogError("Failed to add password for user {UserId}. {@IdentityResult}", userId, removePasswordResult);

                    IdentityError removePasswordError = removePasswordResult.Errors.First();

                    return BadRequest(new
                    {
                        UserId = userId,
                        Reason = removePasswordError.Code,
                        Message = $"Failed to set password for user '{user.UserName}': {removePasswordError.Description}"
                    });
                }
            }

            IdentityResult addPasswordResult = await UserManager.AddPasswordAsync(user, setPassword.NewPassword);
            if (!addPasswordResult.Succeeded)
            {
                Log.LogError("Failed to add password for user {UserId}. {@IdentityResult}", userId, addPasswordResult);

                IdentityError addPasswordError = addPasswordResult.Errors.First();

                return BadRequest(new
                {
                    UserId = userId,
                    Reason = addPasswordError.Code,
                    Message = $"Failed to set password for user '{user.UserName}': {addPasswordError.Description}"
                });
            }

            return Ok(new
            {
                Message = $"Password updated for user '{user.UserName}'."
            });
        }

        /// <summary>
        ///     Create an <see cref="IActionResult"/> representing a user that was not found by Id in the management database.
        /// </summary>
        /// <param name="userId"></param>
        /// <returns>
        ///     The <see cref="IActionResult"/>.
        /// </returns>
        IActionResult UserNotFoundById(string userId)
        {
            Log.LogWarning("User {UserId} not found in the database.", userId);
            
            return NotFound(new
            {
                Id = userId,
                EntityType = "User",
                Message = $"User not found with Id '{userId}'."
            });
        }
    }
}

