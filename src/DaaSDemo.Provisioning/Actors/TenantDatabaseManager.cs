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

    /// <summary>
    ///     Actor that manages a specific tenant database.
    /// </summary>
    public class TenantDatabaseManager
        : ReceiveActorEx
    {
        /// <summary>
        ///     The connection to the tenant's SQL Server instance.
        /// </summary>
        readonly SqlConnection _connection;

        /// <summary>
        ///     A <see cref="DatabaseInstance"/> representing the currently-desired database state.
        /// </summary>
        DatabaseInstance _currentState;

        /// <summary>
        ///     Create a new <see cref="TenantDatabaseManager"/>.
        /// </summary>
        /// <param name="currentState">
        ///     A <see cref="DatabaseInstance"/> representing the currently-desired database state.
        /// </param>
        public TenantDatabaseManager(DatabaseInstance currentState)
        {
            if (currentState == null)
                throw new ArgumentNullException(nameof(currentState));
            
            _connection = new SqlConnection(
                connectionString: $"Data Source={currentState.DatabaseServer.IngressIP},{currentState.DatabaseServer.IngressPort};Initial Catalog=master;User=sa;Password={currentState.DatabaseServer.AdminPassword}"
            );
            _currentState = currentState;

            Receive<DatabaseInstance>(database =>
            {
                _currentState = database;

                if (DoesDatabaseExist())
                    return; // Nothing to do.

                // TODO: Create database.
            });
        }

        protected override void PreStart()
        {
            _connection.Open();
        }

        protected override void PostStop()
        {
            _connection.Close();
            _connection.Dispose();
        }

        bool DoesDatabaseExist()
        {
            using (SqlCommand command = new SqlCommand("Select name from sys.databases Where name = @DatabaseName", _connection))
            {
                command.Parameters.Add("DatabaseName", SqlDbType.NVarChar, size: 100).Value = _currentState.Name;

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    return reader.Read();
                }
            }
        }
    }
}