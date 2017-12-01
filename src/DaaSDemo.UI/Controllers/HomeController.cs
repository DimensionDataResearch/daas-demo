using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

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
        public HomeController(IConfiguration configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));
            
            Configuration = configuration;
        }

        /// <summary>
        ///     The application configuration.
        /// </summary>
        IConfiguration Configuration { get; }

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
        public IActionResult ApiEndPoints()
        {
            return Json(new
            {
                API = Configuration.GetValue<string>("API:EndPoint"),
                STS = Configuration.GetValue<string>("Security:IdentityServerBaseAddress")
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
