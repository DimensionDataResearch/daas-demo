using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using Microsoft.Extensions.Primitives;

namespace DaaSDemo.Api.Controllers
{
    /// <summary>
    ///     The base class for DaaS API controllers.
    /// /// </summary>
    public abstract class ControllerBase
        : Controller
    {
        /// <summary>
        ///     Create a new <see cref="ControllerBase"/>.
        /// </summary>
        /// <param name="logger">
        ///     The controller's logger.
        /// </param>
        protected ControllerBase(ILogger logger)
        {
            if (logger == null)
                throw new ArgumentNullException(nameof(logger));
            
            Log = logger;
        }

        /// <summary>
        ///     The controller's logger.
        /// </summary>
        protected ILogger Log { get; }
    }
}
