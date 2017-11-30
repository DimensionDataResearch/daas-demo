using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DaaSDemo.Models.Data
{
    /// <summary>
    ///     Represents a user in a tenant's database.
    /// </summary>
    [EntitySet("DatabaseUser")]
    public class DatabaseUser
        : IDeepCloneable<DatabaseUser>
    {
        /// <summary>
        ///     The user's Id in the management database.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        ///     The user's name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///     The Id of the database that the user is assigned to.
        /// </summary>
        public string DatabaseId { get; set ;}

        /// <summary>
        ///     The Id of server that hosts the the database that the user is assigned to.
        /// </summary>
        public string ServerId { get; set; }

        /// <summary>
        ///     The Id of the tenant that owns the database that the user is assigned to.
        /// </summary>
        public string TenantId { get; set; }

        /// <summary>
        ///     The user's credentials (if any).
        /// </summary>
        [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Auto)]
        public List<DatabaseUserCredential> Credentials { get; private set; } = new List<DatabaseUserCredential>();

        /// <summary>
        ///     Create a deep clone of the <see cref="DatabaseUser"/>.
        /// </summary>
        /// <returns>
        ///     The cloned <see cref="DatabaseUser"/>.
        /// </returns>
        public DatabaseUser Clone()
        {
            return new DatabaseUser
            {
                Id = Id,
                DatabaseId = DatabaseId,
                ServerId = ServerId,
                TenantId = TenantId,

                Credentials = new List<DatabaseUserCredential>(
                    Credentials.Select(
                        credential => credential.Clone()
                    )
                )
            };
        }
    }
}
