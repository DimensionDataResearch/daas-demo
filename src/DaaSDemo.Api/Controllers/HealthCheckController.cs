using Microsoft.AspNetCore.Mvc;

namespace DaaSDemo.Api.Controllers
{
    /// <summary>
    ///     Controller for health checks.
    /// </summary>
    [Route("health")]
    public class HealthCheckController
        : Controller
    {
        /// <summary>
        ///     Check if the API is alive.
        /// </summary>
        [Route("alive")]    
        public IActionResult Alive() => Ok();
    }
}
