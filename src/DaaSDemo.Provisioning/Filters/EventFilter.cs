using KubeNET.Swagger.Model;

namespace DaaSDemo.Provisioning.Filters
{
    /// <summary>
    ///     The base class for descriptions of how to filter events relating to Kubernetes resources.
    /// </summary>
    public abstract class EventFilter
    {
        /// <summary>
        ///     Determine whether the filter matches the specified metadata.
        /// </summary>
        /// <param name="resourceMetadata">
        ///     The resource metadata to match.
        /// </param>
        /// <returns>
        ///     <c>true</c>, if the filter matches the metadata; otherwise, <c>false</c>.
        /// </returns>
        public abstract bool IsMatch(V1ObjectMeta resourceMetadata);
    }
}
