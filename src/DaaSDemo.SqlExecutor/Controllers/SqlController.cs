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

                using (var sqlCommand = new SqlClient.SqlCommand(command.Sql, sqlConnection))
                {
                    sqlCommand.CommandType = CommandType.Text;

                    // TODO: Add support for parameters.

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
            }

            return Ok(result);
        }

        /// <summary>
        ///     Determine the connection string for the specified <see cref="Command"/>.
        /// </summary>
        /// <param name="command">
        ///     The <see cref="Command"/> being executed.
        /// </param>
        /// <returns>
        ///     The connection string, or <c>null</c> if the connection string could not be determined.
        /// </returns>
        async Task<string> GetConnectionString(Command command)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            Log.LogInformation("Determining connection string for database {DatabaseId} in server {ServerId}...",
                command.DatabaseId,
                command.ServerId
            );

            DatabaseServer targetServer = await Entities.DatabaseServers.FirstOrDefaultAsync(
                server => server.Id == command.ServerId
            );
            if (targetServer == null)
            {
                Log.LogWarning("Cannot determine connection string for database {DatabaseId} in server {ServerId} (server not found).",
                    command.DatabaseId,
                    command.ServerId
                );

                return null;
            }

            V1Service serverService = await KubeClient.ServicesV1.Get(
                name: $"sql-server-{command.ServerId}-service"
            );
            if (serverService == null)
            {
                Log.LogWarning("Cannot determine connection string for database {DatabaseId} in server {ServerId} (server's associated Kubernetes Service not found).",
                    command.DatabaseId,
                    command.ServerId
                );

                return null;
            }

            var connectionStringBuilder = new SqlClient.SqlConnectionStringBuilder
            {
                ["Async"] = "true",
                DataSource = $"tcp:{serverService.Metadata.Name}.{serverService.Metadata.Namespace}.svc.cluster.local,{serverService.Spec.Ports[0].Port}",
            };

            if (command.DatabaseId != MasterDatabaseId)
            {
                DatabaseInstance targetDatabase = await Entities.DatabaseInstances.FirstOrDefaultAsync(
                    database => database.Id == command.DatabaseId
                );
                if (targetDatabase == null)
                {
                    Log.LogWarning("Cannot determine connection string for database {DatabaseId} in server {ServerId} (database not found).",
                        command.DatabaseId,
                        command.ServerId
                    );

                    return null;
                }
                    
                connectionStringBuilder.InitialCatalog = targetDatabase.Name;

                if (command.ExecuteAsAdminUser)
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
                command.DatabaseId,
                connectionStringBuilder.InitialCatalog,
                command.ServerId,
                connectionStringBuilder.DataSource
            );

            return connectionStringBuilder.ConnectionString;
        }
    }
}
