using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Raven.Client.Documents.Session;
using System;

namespace DaaSDemo.Api.Controllers
{
    using Data;

    /// <summary>
    ///     The base class for DaaS API controllers that work with the management database.
    /// /// </summary>
    public abstract class DataControllerBase
        : ControllerBase
    {
        /// <summary>
        ///     Create a new <see cref="ControllerBase"/>.
        /// </summary>
        /// <param name="logger">
        ///     The controller's logger.
        /// </param>
        /// <param name="documentSession">
        ///     The RavenDB document session for the current request.
        /// </param>
        protected DataControllerBase(IDocumentSession documentSession, ILogger logger)
            : base(logger)
        {
            if (documentSession == null)
                throw new ArgumentNullException(nameof(documentSession));
            
            DocumentSession = documentSession;
        }

        /// <summary>
        ///     The RavenDB document session for the current request.
        /// </summary>
        protected IDocumentSession DocumentSession { get; }

        /// <summary>
        ///     The value of the "X-Results-UpTo-ETag" header (if present).
        /// </summary>
        protected long? ResultsUpToETag
        {
            get
            {
                StringValues headerValues;
                if (!Request.Headers.TryGetValue("X-Results-UpTo-ETag", out headerValues) || headerValues.Count != 1)
                    return null;

                string headerValue = headerValues[0];
                if (headerValue.Length < 3 || headerValue[0] != '"' || headerValue[headerValue.Length - 1] != '"')
                    return null;

                return Int64.Parse(
                    headerValue.Substring(1, headerValue.Length - 2)
                );
            }
        }

        /// <summary>
        ///     Populate the "ETag" header using the specified entity's ETag.
        /// </summary>
        /// <param name="entity">
        ///     The target entity.
        /// </param>
        protected void SetEtagHeader<TEntity>(TEntity entity)
            where TEntity: class
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            long etag = DocumentSession.Advanced.GetEtagFor(entity);
            Response.Headers.Add("ETag",
                $"\"{etag}\""
            );
        }

        /// <summary>
        ///     Configure a query to wait for index results up to the ETag (if specified) in the "" header.
        /// </summary>
        /// <param name="queryCustomization">
        ///     The RavenDB query customiser.
        /// </param>
        protected void WaitForResultsUpToRequestedEtag(IDocumentQueryCustomization queryCustomization)
        {
            if (queryCustomization == null)
                throw new ArgumentNullException(nameof(queryCustomization));
            
            long? requestedETag = ResultsUpToETag;
            if (requestedETag.HasValue)
                queryCustomization.WaitForNonStaleResultsAsOf(requestedETag.Value);
        }
    }
}
