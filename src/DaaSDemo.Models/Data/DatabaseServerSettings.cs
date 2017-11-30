using Newtonsoft.Json;

namespace DaaSDemo.Models.Data
{
    /// <summary>
    ///     Well-known settings for a <see cref="DatabaseServer"/>.
    /// </summary>
    public class DatabaseServerSettings
        : IDeepCloneable<DatabaseServerSettings>
    {
        /// <summary>
        ///     The server's storage configuration.
        /// </summary>
        [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Auto)]
        public DatabaseServerStorage Storage { get; set; } = new DatabaseServerStorage();

        /// <summary>
        ///     Create a deep clone of the <see cref="DatabaseServerSettings"/>.
        /// </summary>
        /// <returns>
        ///     The cloned <see cref="DatabaseServerSettings"/>.
        /// </returns>
        public virtual DatabaseServerSettings Clone()
        {
            return new DatabaseServerSettings
            {
                Storage = Storage.Clone()
            };
        }
    }
}
