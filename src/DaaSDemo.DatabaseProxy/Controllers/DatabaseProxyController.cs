using Microsoft.AspNetCore.Mvc;
using System;

namespace DaaSDemo.DatabaseProxy.Controllers
{
    using Filters;

    /// <summary>
    ///     The base class for database proxy controllers.
    /// </summary>
    public abstract class DatabaseProxyController
        : Controller
    {
        /// <summary>
        ///     Create a new <see cref="DatabaseProxyController"/>.
        /// </summary>
        protected DatabaseProxyController()
        {
        }

        /// <summary>
        ///     Create a <see cref="RespondWithException"/> that will cause the specified result to be returned as the response.
        /// </summary>
        /// <param name="result">
        ///     The <see cref="IActionResult"/> to be returned.
        /// </param>
        /// <returns>
        ///     A configured <see cref="RespondWithException"/> which the caller must then throw.
        /// </returns>
        protected RespondWithException RespondWith(IActionResult result)
        {
            if (result == null)
                throw new ArgumentNullException(nameof(result));
            
            return new RespondWithException(result);
        }
    }
}
