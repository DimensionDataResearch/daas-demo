using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;

namespace DaaSDemo.DatabaseProxy.Filters
{
    /// <summary>
    ///     Filter to support <see cref="Controllers.DatabaseProxyController.RespondWith(IActionResult)"/>.
    /// </summary>
    /// <remarks>
    ///     Yesyesyes, it's bad practice to use exceptions for flow-control but in this case it makes the controller logic much more succinct (so I'm comfortable with it).
    /// </remarks>
    public class RespondWithFilter
        : IExceptionFilter
    {
        /// <summary>
        ///     Create a new <see cref="RespondWithFilter"/>.
        /// </summary>
        public RespondWithFilter()
        {
        }

        /// <summary>
        ///     Called when an exception is raised while processing an action.
        /// </summary>
        /// <param name="context">
        ///     Contextual information about the exception.
        /// </param>
        public void OnException(ExceptionContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            
            RespondWithException respondWithException = context.Exception as RespondWithException;
            if (respondWithException == null)
                return;

            context.Result = context.Result;
            context.ExceptionHandled = true;
        }
    }

    /// <summary>
    ///     Exception raised to terminate action processing and respond with a given <see cref="IActionResult"/>.
    /// </summary>
    public class RespondWithException
        : Exception
    {
        /// <summary>
        ///     Create a new <see cref="RespondWithException"/>.
        /// </summary>
        /// <param name="result">
        ///     The <see cref="IActionResult"/> to respond with.
        /// </param>
        public RespondWithException(IActionResult result)
        {
            if (result == null)
                throw new ArgumentNullException(nameof(result));
            
            Result = result;
        }

        /// <summary>
        ///     
        /// </summary>
        IActionResult Result { get; }
    }
}
