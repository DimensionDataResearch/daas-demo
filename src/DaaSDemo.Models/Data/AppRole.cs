using Microsoft.AspNetCore.Identity;
using Newtonsoft.Json;
using System.ComponentModel;
using System.Collections.Generic;

namespace DaaSDemo.Models.Data
{
    /// <summary>
    ///     A DaaS application role.
    /// </summary>
    [EntitySet("AppRole")]
    public class AppRole
        : IdentityRole<string>
    {
        /// <summary>
        ///     The role Id.
        /// </summary>
        public override string Id  => MakeId(NormalizedName);

        /// <summary>
        ///     Claims assigned to all users in this role.
        /// </summary>
        [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Reuse)]
        public HashSet<AppRoleClaim> Claims { get; } = new HashSet<AppRoleClaim>();
        
        /// <summary>
        ///     Make an AppRole document Id.
        /// </summary>
        /// <param name="normalizedName">
        ///     The role's normalised name.
        /// </param>
        /// <returns>
        ///     The document Id.
        /// </returns>
        public static string MakeId(string normalizedName) => $"AppRole/{normalizedName}";
    }

    /// <summary>
    ///     A claim that is granted to all DaaS users in an application role.
    /// </summary>
    public class AppRoleClaim
        : IdentityRoleClaim<string>
    {
        /// <summary>
        ///     Do not persist Id - unused in RavenDB.
        /// </summary>
        [JsonIgnore]
        public override int Id { get; set; }

        /// <summary>
        ///     Do not persist RoleId - unused in RavenDB.
        /// </summary>
        [JsonIgnore]
        public override string RoleId { get; set; }
    }
}
