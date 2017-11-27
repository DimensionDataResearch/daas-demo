using Microsoft.Extensions.Logging;
using System;

namespace DaaSDemo.Provisioning.Provisioners
{
    /// <summary>
    ///     The base class for provisioners.
    /// </summary>
    public abstract class Provisioner
    {
        /// <summary>
        ///     Create a new <see cref="Provisioner"/>.
        /// </summary>
        /// <param name="logger">
        ///     The provisioner's logger.
        /// </param>
        protected Provisioner(ILogger logger)
        {
            if (logger == null)
                throw new ArgumentNullException(nameof(logger));
            
            Log = logger;
        }

        /// <summary>
        ///     The provisioner's logger.
        /// </summary>
        protected ILogger Log { get; }
    }
}
