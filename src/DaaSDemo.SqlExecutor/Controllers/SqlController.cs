using KubeNET.Swagger.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

using SqlClient = System.Data.SqlClient;

namespace DaaSDemo.SqlExecutor.Controllers
{
    using Data;
    using Data.Models;
    using KubeClient;
    using Models.Sql;
    using Newtonsoft.Json.Linq;

    /// <summary>
    ///     Controller for the T-SQL execution API.
    /// </summary>
    [Route("api/v1/sql")]
    public class SqlController
        : Controller
    {
        /// <summary>
        ///     The database Id representing the master database in any server.
        /// </summary>
        const int MasterDatabaseId = 0;

        /// <summary>
        ///     Create a new T-SQL execution API controller.
        /// </summary>
        /// <param name="entities">
        ///     The DaaS entity context.
        /// </param>
        /// <param name="logger">
        ///     The controller's log facility.
        /// </param>
        public SqlController(Entities entities, KubeApiClient kubeClient, ILogger<SqlController> logger)
        {
            if (entities == null)
                throw new ArgumentNullException(nameof(entities));

            if (kubeClient == null)
                throw new ArgumentNullException(nameof(kubeClient));

            if (logger == null)
                throw new ArgumentNullException(nameof(logger));
            
            Entities = entities;
            KubeClient = kubeClient;
            Log = logger;
        }

        /// <summary>
        ///     The DaaS entity context.
        /// </summary>
        Entities Entities { get; }

        /// <summary>
        ///     The controller's log facility.
        /// </summary>
        ILogger Log { get; }

        /// <summary>
        ///     The Kubernetes API client.
        /// </summary>
        KubeApiClient KubeClient { get; }

        /// <summary>
        ///     Execute T-SQL as a command (i.e. non-query).
        /// </summary>
        /// <param name="command">
        ///     A <see cref="Command"/> from the request body, representing the T-SQL to execute.
        /// </param>
        [HttpPost("command")]
        public async Task<IActionResult> ExecuteCommand([FromBody] Command command)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = new CommandResult();

            string connectionString = await GetConnectionString(command);
            if (connectionString == null)
            {
                result.ResultCode = -1;
                result.Errors.Add(new SqlError
                {
                    Kind = SqlErrorKind.Infrastructure,
                    Message = $"Unable to determine connection settings for database {command.DatabaseId} in server {command.ServerId}."
                });

                return BadRequest(result);
            }

            using (SqlClient.SqlConnection sqlConnection = new SqlClient.SqlConnection(connectionString))
            {
                await sqlConnection.OpenAsync();

                sqlConnection.InfoMessage += (sender, args) =>
                {
                    result.Messages.Add(args.Message);
                };

                for (int batchIndex = 0; batchIndex < command.Sql.Count; batchIndex++)
                {
                    string sql = command.Sql[batchIndex];

                    Log.LogInformation("Executing T-SQL batch {BatchNumber} of {BatchCount}...",
                        batchIndex + 1,
                        command.Sql.Count
                    );

                    using (var sqlCommand = new SqlClient.SqlCommand(sql, sqlConnection))
                    {
                        sqlCommand.CommandType = CommandType.Text;

                        foreach (Parameter parameter in command.Parameters)
                        {
                            sqlCommand.Parameters.Add(
                                parameter.ToSqlParameter()
                            );
                        }

                        try
                        {
                            result.ResultCode = await sqlCommand.ExecuteNonQueryAsync();
                        }
                        catch (SqlClient.SqlException sqlException)
                        {
                            Log.LogError(sqlException, "Error while executing T-SQL: {ErrorMessage}", sqlException.Message);

                            result.ResultCode = -1;
                            result.Errors.AddRange(
                                sqlException.Errors.Cast<SqlClient.SqlError>().Select(
                                    error => new SqlError
                                    {
                                        Kind = SqlErrorKind.TSql,
                                        Message = error.Message,
                                        Class = error.Class,
                                        Number = error.Number,
                                        State = error.State,
                                        Procedure = error.Procedure,
                                        Source = error.Source,
                                        LineNumber = error.LineNumber
                                    }
                                )
                            );
                        }
                        catch (Exception unexpectedException)
                        {
                            Log.LogError(unexpectedException, "Unexpected error while executing T-SQL: {ErrorMessage}", unexpectedException.Message);

                            result.ResultCode = -1;
                            result.Errors.Add(new SqlError
                            {
                                Kind = SqlErrorKind.Infrastructure,
                                Message = $"Unexpected error while executing T-SQL: {unexpectedException.Message}"
                            });
                        }
                    }

                    Log.LogInformation("Executed T-SQL batch {BatchNumber} of {BatchCount}.",
                        batchIndex + 1,
                        command.Sql.Count
                    );
                }
            }

            return Ok(result);
        }

        /// <summary>
        ///     Execute T-SQL as a query.
        /// </summary>
        /// <param name="query">
        ///     A <see cref="Query"/> from the request body, representing the T-SQL to execute.
        /// </param>
        [HttpPost("query")]
        public async Task<IActionResult> ExecuteQuery([FromBody] Query query)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var queryResult = new QueryResult();

            string connectionString = await GetConnectionString(query);
            if (connectionString == null)
            {
                queryResult.ResultCode = -1;
                queryResult.Errors.Add(new SqlError
                {
                    Kind = SqlErrorKind.Infrastructure,
                    Message = $"Unable to determine connection settings for database {query.DatabaseId} in server {query.ServerId}."
                });

                return BadRequest(queryResult);
            }

            using (SqlClient.SqlConnection sqlConnection = new SqlClient.SqlConnection(connectionString))
            {
                await sqlConnection.OpenAsync();

                sqlConnection.InfoMessage += (sender, args) =>
                {
                    queryResult.Messages.Add(args.Message);
                };

                for (int batchIndex = 0; batchIndex < query.Sql.Count; batchIndex++)
                {
                    string sql = query.Sql[batchIndex];

                    Log.LogInformation("Executing T-SQL batch {BatchNumber} of {BatchCount}...",
                        batchIndex + 1,
                        query.Sql.Count
                    );

                    using (var sqlCommand = new SqlClient.SqlCommand(sql, sqlConnection))
                    {
                        sqlCommand.CommandType = CommandType.Text;

                        foreach (Parameter parameter in query.Parameters)
                        {
                            sqlCommand.Parameters.Add(
                                parameter.ToSqlParameter()
                            );
                        }

                        try
                        {
                            using (SqlClient.SqlDataReader reader = await sqlCommand.ExecuteReaderAsync())
                            {
                                await ReadResults(reader, queryResult);

                                while (await reader.NextResultAsync())
                                    await ReadResults(reader, queryResult);
                            }

                            queryResult.ResultCode = 0;
                        }
                        catch (SqlClient.SqlException sqlException)
                        {
                            Log.LogError(sqlException, "Error while executing T-SQL: {ErrorMessage}", sqlException.Message);

                            queryResult.ResultCode = -1;
                            queryResult.Errors.AddRange(
                                sqlException.Errors.Cast<SqlClient.SqlError>().Select(
                                    error => new SqlError
                                    {
                                        Kind = SqlErrorKind.TSql,
                                        Message = error.Message,
                                        Class = error.Class,
                                        Number = error.Number,
                                        State = error.State,
                                        Procedure = error.Procedure,
                                        Source = error.Source,
                                        LineNumber = error.LineNumber
                                    }
                                )
                            );
                        }
                        catch (Exception unexpectedException)
                        {
                            Log.LogError(unexpectedException, "Unexpected error while executing T-SQL: {ErrorMessage}", unexpectedException.Message);

                            queryResult.ResultCode = -1;
                            queryResult.Errors.Add(new SqlError
                            {
                                Kind = SqlErrorKind.Infrastructure,
                                Message = $"Unexpected error while executing T-SQL: {unexpectedException.Message}"
                            });
                        }
                    }

                    Log.LogInformation("Executed T-SQL batch {BatchNumber} of {BatchCount}.",
                        batchIndex + 1,
                        query.Sql.Count
                    );
                }
            }

            return Ok(queryResult);
        }

        /// <summary>
        ///     Determine the connection string for the specified <see cref="SqlRequest"/>.
        /// </summary>
        /// <param name="request">
        ///     The <see cref="SqlRequest"/> being executed.
        /// </param>
        /// <returns>
        ///     The connection string, or <c>null</c> if the connection string could not be determined.
        /// </returns>
        async Task<string> GetConnectionString(SqlRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            Log.LogInformation("Determining connection string for database {DatabaseId} in server {ServerId}...",
                request.DatabaseId,
                request.ServerId
            );

            DatabaseServer targetServer = await Entities.DatabaseServers.FirstOrDefaultAsync(
                server => server.Id == request.ServerId
            );
            if (targetServer == null)
            {
                Log.LogWarning("Cannot determine connection string for database {DatabaseId} in server {ServerId} (server not found).",
                    request.DatabaseId,
                    request.ServerId
                );

                return null;
            }

            List<V1Service> matchingServices = await KubeClient.ServicesV1.List(
                labelSelector: $"cloud.dimensiondata.daas.server-id = {targetServer.Id},cloud.dimensiondata.daas.service-type = internal"
            );
            if (matchingServices.Count == 0)
            {
                Log.LogWarning("Cannot determine connection string for database {DatabaseId} in server {ServerId} (server's associated Kubernetes Service not found).",
                    request.DatabaseId,
                    request.ServerId
                );

                return null;
            }

            V1Service serverService = matchingServices[matchingServices.Count - 1];

            var connectionStringBuilder = new SqlClient.SqlConnectionStringBuilder
            {
                DataSource = $"tcp:{serverService.Metadata.Name}.{serverService.Metadata.Namespace}.svc.cluster.local,{serverService.Spec.Ports[0].Port}",
            };

            if (request.DatabaseId != MasterDatabaseId)
            {
                DatabaseInstance targetDatabase = await Entities.DatabaseInstances.FirstOrDefaultAsync(
                    database => database.Id == request.DatabaseId
                );
                if (targetDatabase == null)
                {
                    Log.LogWarning("Cannot determine connection string for database {DatabaseId} in server {ServerId} (database not found).",
                        request.DatabaseId,
                        request.ServerId
                    );

                    return null;
                }
                    
                connectionStringBuilder.InitialCatalog = targetDatabase.Name;

                if (request.ExecuteAsAdminUser)
                {
                    connectionStringBuilder.UserID = "sa";
                    connectionStringBuilder.Password = targetServer.AdminPassword;
                }
                else
                {
                    connectionStringBuilder.UserID = targetDatabase.DatabaseUser;
                    connectionStringBuilder.Password = targetDatabase.DatabasePassword;
                }
            }
            else
            {
                connectionStringBuilder.InitialCatalog = "master";
                
                connectionStringBuilder.UserID = "sa";
                connectionStringBuilder.Password = targetServer.AdminPassword;
            }

            Log.LogInformation("Successfully determined connection string for database {DatabaseId} ({DatabaseName}) in server {ServerId} ({ServerSqlName}).",
                request.DatabaseId,
                connectionStringBuilder.InitialCatalog,
                request.ServerId,
                connectionStringBuilder.DataSource
            );

            return connectionStringBuilder.ConnectionString;
        }

        /// <summary>
        ///     Populate a <see cref="QueryResult"/> with result-sets from the specified <see cref="SqlClient.SqlDataReader"/>.
        /// </summary>
        /// <param name="reader">
        ///     The <see cref="SqlClient.SqlDataReader"/> to read from.
        /// </param>
        /// <param name="queryResult">
        ///     The <see cref="QueryResult"/> to populate.
        /// </param>
        /// <returns>
        ///     A <see cref="Task"/> representing the operation.
        /// </returns>
        async Task ReadResults(SqlClient.SqlDataReader reader, QueryResult queryResult)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));
            
            if (queryResult == null)
                throw new ArgumentNullException(nameof(queryResult));
            
            ResultSet resultSet = new ResultSet();
            queryResult.ResultSets.Add(resultSet);
            while (await reader.ReadAsync())
            {
                var row = new ResultRow();
                resultSet.Rows.Add(row);

                for (int fieldIndex = 0; fieldIndex < reader.FieldCount; fieldIndex++)
                {
                    string fieldName = reader.GetName(fieldIndex);
                    if (!reader.IsDBNull(fieldIndex))
                    {
                        row.Columns[fieldName] = new JValue(
                            reader.GetValue(fieldIndex)
                        );
                    }
                    else
                        row.Columns[fieldName] = null;
                }
            }
        }
    }
}
