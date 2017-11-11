using Microsoft.AspNetCore.Mvc;
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
    public class HomeController
        : Controller
    {
        /// <summary>
        ///     Display the main application page.
        /// </summary>
        public IActionResult Index() => View();

        /// <summary>
        ///     Display the error page.
        /// </summary>
        public IActionResult Error()
        {
            ViewData["RequestId"] = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
            
            return View();
        }
    }
}
