using IdentityModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

using SignInResult = Microsoft.AspNetCore.Identity.SignInResult;

namespace DaaSDemo.Api.Controllers
{
    using Models.Api;
    using Models.Data;

    /// <summary>
    ///     Controller for the DaaS User Identity API.
    /// </summary>
    [Authorize("User")]
    [Route("api/v1/identity")]
    public class IdentityController
        : ControllerBase
    {
        /// <summary>
        ///     Create a new <see cref="IdentityController"/>.
        /// </summary>
        /// <param name="userManager">
        ///     The ASP.NET Core Identity user manager.
        /// </param>
        /// <param name="signInManager">
        ///     The ASP.NET Core Identity sign-in manager.
        /// </param>
        /// <param name="logger">
        ///     The controller's logger.
        /// </param>
        public IdentityController(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager, ILogger<IdentityController> logger)
            : base(logger)
        {
            if (userManager == null)
                throw new ArgumentNullException(nameof(userManager));

            if (signInManager == null)
                throw new ArgumentNullException(nameof(signInManager));
            
            UserManager = userManager;
            SignInManager = signInManager;
        }

        /// <summary>
        ///     The ASP.NET Core Identity user manager.
        /// </summary>
        UserManager<AppUser> UserManager { get; }

        /// <summary>
        ///     The ASP.NET Core Identity sign-in manager.
        /// </summary>
        SignInManager<AppUser> SignInManager { get; }

        /// <summary>
        ///     Get information about the current (authenticated) user.
        /// </summary>
        [HttpGet("me")]
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

        /// <summary>
        ///     Change the current user's password.
        /// </summary>
        /// <param name="changePassword">
        ///     The change-user-password model.
        /// </param>
        [HttpPost("password"), Authorize("User")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePassword changePassword)
        {
            if (String.Equals(changePassword.NewPassword, changePassword.NewPasswordConfirmation, StringComparison.OrdinalIgnoreCase))
                ModelState.AddModelError("NewPasswordConfirmation", "Passwords do not match.");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            string userId = User.FindFirstValue(JwtClaimTypes.Subject) ?? "<unknown>";

            AppUser user = await UserManager.GetUserAsync(User);
            if (user == null)
            {
                Log.LogWarning("User {UserId} not found in the database.", userId);

                return BadRequest(new
                {
                    UserId = userId,
                    Reason = "UserNotFound",
                    Message = $"User '{User.Identity.Name}' not found."
                });
            }

            SignInResult signInResult = await SignInManager.CheckPasswordSignInAsync(user, changePassword.CurrentPassword, lockoutOnFailure: true);
            if (!signInResult.Succeeded)
            {
                if (signInResult.IsLockedOut)
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new
                    {
                        Reason = "AccountLocked",
                        Message = $"Account locked out for user '{user.UserName}'."
                    });
                }
                
                // TODO: Check other flags on SignInResult - otherwise we may hide the real reason they can't change their passsword.

                return StatusCode(StatusCodes.Status403Forbidden, new
                {
                    Reason = "PasswordIncorrect",
                    Message = $"Current password for user '{user.UserName}' is incorrect."
                });
            }

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
                        Message = $"Failed to change password for user '{user.UserName}': {removePasswordError.Description}"
                    });
                }
            }

            IdentityResult addPasswordResult = await UserManager.AddPasswordAsync(user, changePassword.NewPassword);
            if (!addPasswordResult.Succeeded)
            {
                Log.LogError("Failed to add password for user {UserId}. {@IdentityResult}", userId, addPasswordResult);

                IdentityError addPasswordError = addPasswordResult.Errors.First();

                return BadRequest(new
                {
                    UserId = userId,
                    Reason = addPasswordError.Code,
                    Message = $"Failed to change password for user '{user.UserName}': {addPasswordError.Description}"
                });
            }

            return Ok(new
            {
                Message = $"Password changed for user '{user.UserName}'."
            });
        }
    }
}
