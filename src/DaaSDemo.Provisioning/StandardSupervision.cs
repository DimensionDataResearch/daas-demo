using Akka.Actor;
using Raven.Client.Exceptions;
using System;

namespace DaaSDemo.Provisioning
{
    using Exceptions;

    /// <summary>
    ///     Standard actor-supervision strategies used by the provisioning engine.
    /// </summary>
    public static class StandardSupervision
    {
        /// <summary>
        ///     The default supervisor strategy.
        /// </summary>
        public static SupervisorStrategy Default => new OneForOneStrategy(
            maxNrOfRetries: 3,
            withinTimeRange: TimeSpan.FromSeconds(5),
            decider: Decider.From(exception =>
            {
                if (exception is FatalProvisioningException)
                    return Directive.Escalate;

                if (exception is ProvisioningException)
                    return Directive.Restart;

                if (exception is RavenException)
                    return Directive.Restart;

                return Directive.Stop;
            })
        );
    }
}
