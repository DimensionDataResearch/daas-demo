using HTTPlease;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Security;
using System.Threading;
using System.Threading.Tasks;

namespace DaaSDemo.SqlExecutor.Client
{
    using Models.Sql;

    /// <summary>
    ///     Client for the SQL Executor API.
    /// </summary>
    /// <remarks>
    ///     TODO: Add authentication.
    /// </remarks>
    public sealed class SqlApiClient
        : IDisposable
    {
        /// <summary>
        ///     The database Id representing the "master" database in any server.
        /// </summary>
        public static readonly int MasterDatabaseId = 0;

        /// <summary>
        ///     Create a new <see cref="SqlApiClient"/>.
        /// </summary>
        /// <param name="httpClient">
        ///     The underlying HTTP client.
        /// </param>
        SqlApiClient(HttpClient httpClient)
        {
            if (httpClient == null)
                throw new ArgumentNullException(nameof(httpClient));

            Http = httpClient;
        }

        /// <summary>
        ///     Dispose of resources being used by the <see cref="SqlApiClient"/>.
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
        public async Task<CommandResult> ExecuteCommand(int serverId, int databaseId, IEnumerable<string> sql, IEnumerable<Parameter> parameters = null, bool executeAsAdminUser = false, CancellationToken cancellationToken = default)
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
                await Http.PostAsJsonAsync(Requests.Command,
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
        public async Task<QueryResult> ExecuteQuery(int serverId, int databaseId, IEnumerable<string> sql, IEnumerable<Parameter> parameters = null, bool executeAsAdminUser = false, CancellationToken cancellationToken = default)
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
                await Http.PostAsJsonAsync(Requests.Query,
                    postBody: query,
                    cancellationToken: cancellationToken
                )
                .ReadContentAsAsync<QueryResult, JObject>();
        }

        /// <summary>
        ///     Create a new <see cref="SqlApiClient"/>.
        /// </summary>
        /// <param name="endPointUri">
        ///     The base address for the Kubernetes API end-point.
        /// </param>
        /// <returns>
        ///     The configured <see cref="SqlApiClient"/>.
        /// </returns>
        public static SqlApiClient Create(Uri endPointUri)
        {
            if (endPointUri == null)
                throw new ArgumentNullException(nameof(endPointUri));
            
            return new SqlApiClient(
                new HttpClient { BaseAddress = endPointUri }
            );
        }

        /// <summary>
        ///     Request definitions for the SQL Executor API.
        /// </summary>
        public static class Requests
        {
            /// <summary>
            ///     The base request definition for the SQL Excecutor API.
            /// </summary>
            public static HttpRequest Api = HttpRequest.Factory.Json("api/v1/sql");

            /// <summary>
            ///     Invoke T-SQL as a command.
            /// </summary>
            public static HttpRequest Command = Api.WithRelativeUri("command");

            /// <summary>
            ///     Invoke T-SQL as a query.
            /// </summary>
            public static HttpRequest Query = Api.WithRelativeUri("query");
        }
    }
}
