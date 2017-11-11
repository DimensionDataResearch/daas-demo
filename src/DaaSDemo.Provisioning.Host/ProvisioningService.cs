using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DaaSDemo.Provisioning.Host
{
    /// <summary>
    ///     A <see cref="IHostedService"/> wrapper for the <see cref="ProvisioningEngine"/>.
    /// </summary>
    class ProvisioningService
        : IHostedService
    {
        /// <summary>
        ///     The underlying <see cref="ProvisioningEngine"/>.
        /// </summary>
        readonly ProvisioningEngine _engine;

        /// <summary>
        ///     Create a new <see cref="ProvisioningService"/>.
        /// </summary>
        /// <param name="engine">
        ///     The underlying <see cref="ProvisioningEngine"/>.
        /// </param>
        public ProvisioningService(ProvisioningEngine engine)
        {
            if (engine == null)
                throw new ArgumentNullException(nameof(engine));
            
            _engine = engine;
        }

        /// <summary>
        ///     Start the service.
        /// </summary>
        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken"/> that can be used to cancel the operation.
        /// </param>
        /// <returns>
        ///     A <see cref="Task"/> representing the operation.
        /// </returns>
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _engine.Start();

            return Task.CompletedTask;
        }

        /// <summary>
        ///     Stop the service.
        /// </summary>
        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken"/> that can be used to cancel the operation.
        /// </param>
        /// <returns>
        ///     A <see cref="Task"/> representing the operation.
        /// </returns>
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await _engine.Stop();
        }
    }
}
