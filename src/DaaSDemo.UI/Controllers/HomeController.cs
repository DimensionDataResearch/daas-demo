using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using DaaSDemo.Models.Data;

namespace DaaSDemo.UI.Controllers
{
    /// <summary>
    ///     The default ("Home") controller.
    /// </summary>
    [Route("")]
    public class HomeController
        : Controller
    {
        /// <summary>
        ///     Create a new <see cref="HomeController"/>.
        /// </summary>
        /// <param name="configuration">
        ///     The application configuration.
        /// </param>
        public HomeController(IConfiguration configuration, UserManager<AppUser> userManager)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));
            
            Configuration = configuration;
            UserManager = userManager;
        }

        /// <summary>
        ///     The application configuration.
        /// </summary>
        IConfiguration Configuration { get; }

        UserManager<AppUser> UserManager { get; }

        /// <summary>
        ///     Display the main application page.
        /// </summary>
        [Route("")]
        public IActionResult Index() => RedirectToAction("App");

        /// <summary>
        ///     Display the main application page.
        /// </summary>
        [Route("app")]
        public IActionResult App() => View("Index");

        /// <summary>
        ///     Get configured API end-points.
        /// </summary>
        [HttpGet("end-points")]
        public async Task<IActionResult> ApiEndPoints()
        {
            AppUser user = await UserManager.FindByNameAsync("TINTOY");
            if (user != null)
            {
                Serilog.Log.Information("Found user {UserName} with hashed password {PasswordHash}.", user.UserName, user.PasswordHash);

                bool isMatch = await UserManager.CheckPasswordAsync(user, "woozle");
                if (!isMatch)
                {
                    Serilog.Log.Warning("Password does not match for user {UserName}.", user.PasswordHash);

                    var result = await UserManager.RemovePasswordAsync(user);
                    if (result.Succeeded)
                    {
                        Serilog.Log.Information("Removed password for user {UserName}.", user.UserName);

                        result = await UserManager.AddPasswordAsync(user, "woozle");
                        if (result.Succeeded)
                            Serilog.Log.Information("Added password for user {UserName}.", user.UserName);
                        else
                            Serilog.Log.Warning("Failed to add password for user {UserName}: {@Result}", user.UserName, result);
                    }
                    else
                        Serilog.Log.Warning("Failed to remove password for user {UserName}: {@Result}", user.UserName, result);
                }
                else
                    Serilog.Log.Information("Found user {UserName} with matching hashed password {PasswordHash}.", user.PasswordHash);
            }
            else
                Serilog.Log.Warning("Failed to retrieve user.");

            return Json(new
            {
                API = Configuration.GetValue<string>("API:EndPoint"),
                IdentityServer = Configuration.GetValue<string>("Security:IdentityServerBaseAddress")
            });
        }

        // TODO: Implement /end-points and return both API and STS base addresses.

        /// <summary>
        ///     Display the error page.
        /// </summary>
        [Route("error")]
        public IActionResult Error()
        {
            ViewData["RequestId"] = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
            
            return View();
        }
    }
}
