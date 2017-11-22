using System;
using System.Text.RegularExpressions;

namespace DaaSDemo.Provisioning
{
    using Models.Data;
    
    /// <summary>
    ///     Naming strategies for Kubernetes resources.
    /// </summary>
    public class KubeNames
    {
        /// <summary>
        ///     Regular expression for splitting PascalCase words.
        /// </summary>
        static Regex PascalCaseSplitter = new Regex(@"([a-z][A-Z])");

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
            
            // e.g. "DatabaseServer-1-A" -> "database-server-1-A"
            id = PascalCaseSplitter.Replace(id, match =>
            {
                return String.Concat(
                    match.Groups[0].Value[0],
                    '-',
                    Char.ToLowerInvariant(
                        match.Groups[0].Value[1]
                    )
                );
            });

            // e.g. "database-server-1-A" -> "database-server-1-a"
            id = id.ToLowerInvariant();

            return id;
        }
    }
}
