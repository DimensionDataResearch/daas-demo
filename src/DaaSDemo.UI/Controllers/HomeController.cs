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
        ///     Get client app configuration.
        /// </summary>
        [HttpGet("config")]
        public IActionResult ApiEndPoints()
        {
            return Json(new
            {
                API = new
                {
                    EndPoint = Configuration.GetValue<string>("API:EndPoint")
                },
                Identity = new
                {
                    Authority = Configuration.GetValue<string>("Security:Authority"),
                    ClientId = Configuration.GetValue<string>("Security:ClientId:UI"),
                    AdditionalScopes = Configuration.GetValue<string>("Security:AdditionalScopes")
                }
            });
        }

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
