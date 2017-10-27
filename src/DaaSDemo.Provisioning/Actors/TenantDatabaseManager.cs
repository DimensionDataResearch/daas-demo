using Akka;
using Akka.Actor;
using HTTPlease;
using KubeNET.Swagger.Model;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace DaaSDemo.Provisioning.Actors
{
    using Data;
    using Data.Models;
    using Messages;

    /// <summary>
    ///     Actor that manages a specific tenant database.
    /// </summary>
    public class TenantDatabaseManager
        : ReceiveActorEx
    {
        /// <summary>
        ///     A reference to the <see cref="DataAccess"/> actor.
        /// </summary>
        readonly IActorRef _dataAccess;

        /// <summary>
        ///     The connection to the tenant's SQL Server instance.
        /// </summary>
        SqlConnection _connection;

        /// <summary>
        ///     A <see cref="DatabaseInstance"/> representing the currently-desired database state.
        /// </summary>
        DatabaseInstance _currentState;

        /// <summary>
        ///     Create a new <see cref="TenantDatabaseManager"/>.
        /// </summary>
        /// <param name="dataAccess">
        ///     A reference to the <see cref="DataAccess"/> actor.
        /// </param>
        public TenantDatabaseManager(IActorRef dataAccess)
        {
            if (dataAccess == null)
                throw new ArgumentNullException(nameof(dataAccess));

            _dataAccess = dataAccess;

            Receive<DatabaseInstance>(database =>
            {
                _currentState = database;

                Log.Info("Received database configuration (Id:{DatabaseId}, Name:{DatabaseName}).",
                    database.Id,
                    database.Name
                );

                switch (database.Action)
                {
                    case ProvisioningAction.Provision:
                    {
                        _dataAccess.Tell(
                            new DatabaseProvisioning(database.Id)
                        );

                        if (!DoesDatabaseExist())
                        {
                            // TODO: Create database.
                        }
                        else
                        {
                            Log.Info("Database {DatabaseName} already exists; will treat as provisioned.",
                                database.Id,
                                database.Name
                            );
                        }

                        _dataAccess.Tell(
                            new DatabaseProvisioned(database.Id)
                        );

                        break;
                    }
                    case ProvisioningAction.Deprovision:
                    {
                        _dataAccess.Tell(
                            new DatabaseDeprovisioning(database.Id)
                        );

                        if (DoesDatabaseExist())
                        {
                            // TODO: Drop database.
                        }
                        else
                        {
                            Log.Info("Database {DatabaseName} not found; will treat as deprovisioned.",
                                database.Id,
                                database.Name
                            );
                        }

                        _dataAccess.Tell(
                            new DatabaseDeprovisioned(database.Id)
                        );

                        Context.Stop(Self);

                        break;
                    }
                }
            });
        }

        /// <summary>
        ///     Called when the actor is stopped.
        /// </summary>
        protected override void PostStop()
        {
            if (_connection != null)
            {
                _connection.Close();
                _connection.Dispose();

                _connection = null;
            }
        }

        /// <summary>
        ///     Ensure that the database connection is open.
        /// </summary>
        void EnsureConnection()
        {
            if (_connection == null)
            {
                _connection = new SqlConnection(
                    _currentState.GetConnectionString()
                );
            }

            if (_connection.State == ConnectionState.Closed)
                _connection.Open();
        }

        /// <summary>
        ///     Check if the target database exists.
        /// </summary>
        /// <returns>
        ///     <c>true</c>, if the database exists; otherwise, <c>false</c>.
        /// </returns>
        bool DoesDatabaseExist()
        {
            EnsureConnection();

            using (SqlCommand command = new SqlCommand("Select name from sys.databases Where name = @DatabaseName", _connection))
            {
                command.Parameters.Add("DatabaseName", SqlDbType.NVarChar, size: 100).Value = _currentState.Name;

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    return reader.Read();
                }
            }
        }

        /// <summary>
        ///     Get the name of the <see cref="TenantServerManager"/> actor for the specified tenant.
        /// </summary>
        /// <param name="tenantId">
        ///     The tenant Id.
        /// </param>
        /// <returns>
        ///     The actor name.
        /// </returns>
        public static string ActorName(int tenantId) => $"database-manager.{tenantId}";
    }
}