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
        : IDeepCloneable<AppRole>
    {
        /// <summary>
        ///     The role Id.
        /// </summary>
        public string Id  => MakeId(Name);

        /// <summary>
        ///     The role name (must be a single word, all lower-case).
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///     The role's display name.
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        ///     Create a deep clone of the <see cref="AppRole"/>.
        /// </summary>
        /// <returns>
        ///     The cloned <see cref="AppRole"/>.
        /// </returns>
        public AppRole Clone()
        {
            return new AppRole
            {
                Name = Name,
                DisplayName = DisplayName
            };
        }
        
        /// <summary>
        ///     Make an AppRole document Id.
        /// </summary>
        /// <param name="name">
        ///     The role name.
        /// </param>
        /// <returns>
        ///     The document Id.
        /// </returns>
        public static string MakeId(string name) => $"app-role/{name}";
    }
}
