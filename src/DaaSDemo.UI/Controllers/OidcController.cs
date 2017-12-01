using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DaaSDemo.UI.Controllers
{
    /// <summary>
    ///     Controller for OIDC authentication / authorisation.
    /// </summary>
    [Route("oidc")]
    public class OidcController
        : Controller
    {
        /// <summary>
        ///     Handle OIDC signin callback via in-browser popup.
        /// </summary>
        [Route("signin/popup")]
        public IActionResult SigninPopupCallback() => View();

        /// <summary>
        ///     Handle OIDC signin callback via iframe.
        /// </summary>
        [Route("signin/silent")]
        public IActionResult SigninSilentCallback() => View();
    }
}
