using HTTPlease;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Security;
using System.Threading;
using System.Threading.Tasks;

namespace DaaSDemo.DatabaseProxy.Client
{
    using Models.DatabaseProxy;

    /// <summary>
    ///     Client for the Database Proxy API.
    /// </summary>
    /// <remarks>
    ///     TODO: Add authentication.
    /// </remarks>
    public sealed class DatabaseProxyApiClient
        : IDisposable
    {
        /// <summary>
        ///     The database Id representing the "master" database in any server.
        /// </summary>
        public static readonly string MasterDatabaseId = "master";

        /// <summary>
        ///     Create a new <see cref="DatabaseProxyApiClient"/>.
        /// </summary>
        /// <param name="httpClient">
        ///     The underlying HTTP client.
        /// </param>
        DatabaseProxyApiClient(HttpClient httpClient)
        {
            if (httpClient == null)
                throw new ArgumentNullException(nameof(httpClient));

            Http = httpClient;
        }

        /// <summary>
        ///     Dispose of resources being used by the <see cref="DatabaseProxyApiClient"/>.
        /// </summary>
        public void Dispose() => Http?.Dispose();

        /// <summary>
        ///     The underlying HTTP client.
        /// </summary>
        HttpClient Http { get; }

        /// <summary>
        ///     Request execution of T-SQL as a command (i.e. non-query).
        /// </summary>
        /// <param name="serverId">
        ///     The Id of the target SQL server.
        /// </param>
        /// <param name="databaseId">
        ///     The Id of the target database (or <see cref="MasterDatabaseId"/> for the "master" database).
        /// </param>
        /// <param name="sql">
        ///     The T-SQL to execute.
        /// </param>
        /// <param name="parameters">
        ///     The query parameters (if any).
        /// </param>
        /// <param name="executeAsAdminUser">
        ///     Execute the command as the server's admin user ("sa")?
        /// </param>
        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken"/> that can be used to cancel the request.
        /// </param>
        /// <returns>
        ///     The command result.
        /// </returns>
        public async Task<CommandResult> ExecuteCommand(string serverId, string databaseId, IEnumerable<string> sql, IEnumerable<Parameter> parameters = null, bool executeAsAdminUser = false, CancellationToken cancellationToken = default)
        {
            if (sql == null)
                throw new ArgumentNullException(nameof(sql));

            var command = new Command
            {
                ServerId = serverId,
                DatabaseId = databaseId,
                ExecuteAsAdminUser = executeAsAdminUser
            };
            command.Sql.AddRange(sql);
            if (parameters != null)
                command.Parameters.AddRange(parameters);

            return
                await Http.PostAsJsonAsync(Requests.SqlCommand,
                    postBody: command,
                    cancellationToken: cancellationToken
                )
                .ReadContentAsAsync<CommandResult, JObject>();
        }

        /// <summary>
        ///     Request execution of T-SQL as a query (i.e. non-query).
        /// </summary>
        /// <param name="serverId">
        ///     The Id of the target SQL server.
        /// </param>
        /// <param name="databaseId">
        ///     The Id of the target database (or <see cref="MasterDatabaseId"/> for the "master" database).
        /// </param>
        /// <param name="sql">
        ///     The T-SQL to execute.
        /// </param>
        /// <param name="parameters">
        ///     The query parameters (if any).
        /// </param>
        /// <param name="executeAsAdminUser">
        ///     Execute the query as the server's admin user ("sa")?
        /// </param>
        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken"/> that can be used to cancel the request.
        /// </param>
        /// <returns>
        ///     The query result.
        /// </returns>
        public async Task<QueryResult> ExecuteQuery(string serverId, string databaseId, IEnumerable<string> sql, IEnumerable<Parameter> parameters = null, bool executeAsAdminUser = false, CancellationToken cancellationToken = default)
        {
            if (sql == null)
                throw new ArgumentNullException(nameof(sql));

            var query = new Query
            {
                ServerId = serverId,
                DatabaseId = databaseId,
                ExecuteAsAdminUser = executeAsAdminUser
            };
            query.Sql.AddRange(sql);
            if (parameters != null)
                query.Parameters.AddRange(parameters);

            return
                await Http.PostAsJsonAsync(Requests.SqlQuery,
                    postBody: query,
                    cancellationToken: cancellationToken
                )
                .ReadContentAsAsync<QueryResult, JObject>();
        }

        /// <summary>
        ///     Request initialisation of configuration for a RavenDB server.
        /// </summary>
        /// <param name="serverId">
        ///     The Id of the target server.
        /// </param>
        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken"/> that can be used to cancel the request.
        /// </param>
        /// <returns>
        ///     A <see cref="Task"/> representing the operation.
        /// </returns>
        public async Task InitializeRavenServerConfiguration(string serverId, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(serverId))
                throw new ArgumentException("Argument cannot be null, empty, or entirely composed of whitespace: 'serverId'.", nameof(serverId));
            
            HttpResponseMessage response = await Http.PostAsync(
                Requests.RavenInitializeConfiguration.WithTemplateParameters(new
                {
                    ServerId = serverId
                }),
                cancellationToken: cancellationToken
            );
            using (response)
            {
                response.EnsureSuccessStatusCode();
            }
        }

        /// <summary>
        ///     Create a new <see cref="DatabaseProxyApiClient"/>.
        /// </summary>
        /// <param name="endPointUri">
        ///     The base address for the Kubernetes API end-point.
        /// </param>
        /// <returns>
        ///     The configured <see cref="DatabaseProxyApiClient"/>.
        /// </returns>
        public static DatabaseProxyApiClient Create(Uri endPointUri)
        {
            if (endPointUri == null)
                throw new ArgumentNullException(nameof(endPointUri));
            
            return new DatabaseProxyApiClient(
                new HttpClient { BaseAddress = endPointUri }
            );
        }

        /// <summary>
        ///     Request definitions for the Database Proxy API.
        /// </summary>
        public static class Requests
        {
            /// <summary>
            ///     The base request definition for the SQL proxy API.
            /// </summary>
            public static HttpRequest SqlApi = HttpRequest.Factory.Json("api/v1/sql");

            /// <summary>
            ///     Invoke T-SQL as a command.
            /// </summary>
            public static HttpRequest SqlCommand = SqlApi.WithRelativeUri("command");

            /// <summary>
            ///     Invoke T-SQL as a query.
            /// </summary>
            public static HttpRequest SqlQuery = SqlApi.WithRelativeUri("query");

            /// <summary>
            ///     The base request definition for the RavenDB proxy API.
            /// </summary>
            public static HttpRequest RavenApi = HttpRequest.Factory.Json("api/v1/raven");

            /// <summary>
            ///     Initialize RavenDB server configuration.
            /// </summary>
            public static HttpRequest RavenInitializeConfiguration = RavenApi.WithRelativeUri("{ServerId}/initialize");
        }
    }
}
