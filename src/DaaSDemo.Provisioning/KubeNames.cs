using System;

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
        public virtual string BaseName(DatabaseServer server) => SafeId(server.Id);

        /// <summary>
        ///     Get the name of the PersistentVolumeClaim used for storing SQL Server data.
        /// </summary>
        /// <param name="server">
        ///     A <see cref="DatabaseServer"/> representing the target server
        /// </param>
        /// <returns>
        ///     The PersistentVolumeClaim name.
        /// </returns>
        public virtual string DataVolumeClaim(DatabaseServer server) => $"{BaseName(server)}-data";

        /// <summary>
        ///     Transform the specified Id so that it's safe for use in Kubernetes resource names.
        /// </summary>
        /// <param name="id">
        ///     The Id.
        /// </param>
        /// <returns>
        ///     The safe-for-Kubernetes name.
        /// </returns>
        public virtual string SafeId(string id)
        {
            if (String.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Argument cannot be null, empty, or entirely composed of whitespace: 'id'.", nameof(id));
            
            id = id.ToLower();
            
            // Is the Id in "EntitySetName-1234-XXX" format ?
            string[] idComponents = id.Split('-');
            if (idComponents.Length == 3)
                return idComponents[1];

            return id;
        }
    }
}
