using Microsoft.AspNetCore.Identity;
using Newtonsoft.Json;

namespace DaaSDemo.Models.Data
{
    /// <summary>
    ///     Represents a claim assigned to a DaaS application user.
    /// </summary>
    public class AppUserClaim
        : IdentityUserClaim<string>
    {
        /// <summary>
        ///     Do not persist Id - unused in RavenDB.
        /// </summary>
        [JsonIgnore]
        public override int Id { get; set; }
    }
}
