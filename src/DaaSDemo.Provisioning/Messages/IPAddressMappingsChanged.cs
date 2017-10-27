using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace DaaSDemo.Provisioning.Messages
{
    using Data.Models;

    /// <summary>
    ///     Message notifying the recipient that IP address mappings for Kubernetes nodes have changed.
    /// </summary>
    public class IPAddressMappingsChanged
    {
        /// <summary>
        ///     Create a new <see cref="IPAddressMappingsChanged"/> message.
        /// </summary>
        /// <param name="mappings">
        ///     The mappings from internal IP addresses to external IP addresses.
        /// </param>
        public IPAddressMappingsChanged(IEnumerable<IPAddressMapping> mappings)
        {
            if (mappings == null)
                throw new ArgumentNullException(nameof(mappings));
            
            Mappings = mappings.ToImmutableDictionary(
                mapping => mapping.InternalIP,
                mapping => mapping.ExternalIP
            );
        }

        /// <summary>
        ///     The mappings from internal IP addresses to external IP addresses.
        /// </summary>
        public ImmutableDictionary<string, string> Mappings { get; }
    }
}