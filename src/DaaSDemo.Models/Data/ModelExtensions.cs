using System;
using System.Collections.Generic;

namespace DaaSDemo.Models.Data
{
    /// <summary>
    ///     Extension methods for DaaS data models.
    /// </summary>
    public static class ModelExtensions
    {
        /// <summary>
        ///     Get the server settings, cast to the specified type.
        /// </summary>
        /// <typeparam name="TSettings">
        ///     The server settings type.
        /// </typeparam>
        /// <returns>
        ///     The settings.
        /// </returns>
        public static TSettings GetSettings<TSettings>(this DatabaseServer server)
            where TSettings : DatabaseServerSettings
        {
            if (server == null)
                throw new ArgumentNullException(nameof(server));

            if (server.Settings is TSettings typedSettings)
                return typedSettings;

            if (server.Settings != null)
                throw new InvalidOperationException($"Expected DatabaseServerSettings of type '{typeof(TSettings).Name}' but found '{server.Settings.GetType().Name}' instead.");

            throw new InvalidOperationException("DatabaseServer has null DatabaseServerSettings.");
        }

        /// <summary>
        ///     Add a provisioning event by capturing the current server state.
        /// </summary>
        /// <param name="messages">
        ///     Messages (if any) to associate with the event.
        /// </param>
        public static void AddProvisioningEvent(this DatabaseServer server, params string[] messages)
            => server.AddProvisioningEvent((IEnumerable<string>)messages);

        /// <summary>
        ///     Add a provisioning event by capturing the current server state.
        /// </summary>
        /// <param name="messages">
        ///     Messages (if any) to associate with the event.
        /// </param>
        public static void AddProvisioningEvent(this DatabaseServer server, IEnumerable<string> messages)
        {
            if (messages == null)
                throw new ArgumentNullException(nameof(messages));

            var provisioningEvent = new DatabaseServerProvisioningEvent
            {
                Timestamp = DateTimeOffset.Now,
                Action = server.Action,
                Phase = server.Phase,
                Status = server.Status,
            };
            provisioningEvent.Messages.AddRange(messages);
            
            server.Events.Add(provisioningEvent);
        }

        /// <summary>
        ///     Add an ingress-change event by capturing the current server state.
        /// </summary>
        public static void AddIngressChangedEvent(this DatabaseServer server)
        {
            var ingressChangedEvent = new DatabaseServerIngressChangedEvent
            {
                Timestamp = DateTimeOffset.Now,
                PublicFQDN = server.PublicFQDN,
                PublicPort = server.PublicPort
            };
            if (server.PublicFQDN == null || server.PublicPort == null)
                ingressChangedEvent.Messages.Add("Server is not externally-accessible.");
            else
                ingressChangedEvent.Messages.Add($"Server is externally-accessible on '{ingressChangedEvent.PublicFQDN}:{ingressChangedEvent.PublicPort}'.");

            server.Events.Add(ingressChangedEvent);
        }
    }
}
