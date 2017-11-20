namespace DaaSDemo.Provisioning
{
    using Models.Data;
    
    /// <summary>
    ///     Naming strategies for Kubernetes resources.
    /// </summary>
    public class KubeNames
    {
        /// <summary>
        ///     Create a new <see cref="KubeNames"/>.
        /// </summary>
        public KubeNames()
        {
        }

        /// <summary>
        ///     Get the base resource name for the specified server.
        /// </summary>
        /// <param name="server">
        ///     A <see cref="DatabaseServer"/> representing the target server
        /// </param>
        /// <returns>
        ///     The base resource name.
        /// </returns>
        public virtual string BaseName(DatabaseServer server) => $"sql-server-{server.Id}";
    }
}
