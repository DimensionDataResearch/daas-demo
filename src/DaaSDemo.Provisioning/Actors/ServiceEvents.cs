using Akka.Actor;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace DaaSDemo.Provisioning.Actors
{
    using Common.Utilities;
    using Filters;
    using KubeClient;
    using KubeClient.Models;
    using Messages;

    // TODO: Consider adding reference-count for subscriptions and only watch for events from the Kubernetes API while there are active subscribers.
    // TODO: Consider implementing messages to pause / resume watching for events.

    /// <summary>
    ///     Actor that publishes events relating to Kubernetes Services in the default namespace.
    /// </summary>
    public class ServiceEvents
        : ReceiveActorEx
    {
        /// <summary>
        ///     An <see cref="IDisposable"/> representing the underlying subscription to the Kubernetes API.
        /// </summary>
        IDisposable _eventSourceSubscription;

        /// <summary>
        ///     Create a new <see cref="ServiceEvents"/> actor.
        /// </summary>
        /// <param name="kubeClient">
        ///     The <see cref="KubeApiClient"/> used to communicate with the Kubernetes API.
        /// </param>
        public ServiceEvents(KubeApiClient kubeClient)
        {
            if (kubeClient == null)
                throw new ArgumentNullException(nameof(kubeClient));
            
            KubeClient = kubeClient;
            EventSource = KubeClient.ServicesV1().WatchAll(
                kubeNamespace: "default"
            );

            Receive<ResourceEventV1<ServiceV1>>(resourceEvent =>
            {
                EventBus.Publish(resourceEvent);
            });
            Receive<SubscribeResourceEvents<ResourceEventFilter>>(subscribe =>
            {
                EventBus.Subscribe(Sender, subscribe.Filter);
            });
            Receive<UnsubscribeResourceEvents<ResourceEventFilter>>(unsubscribe =>
            {
                EventBus.Unsubscribe(Sender, unsubscribe.Filter);
            });
            Receive<UnsubscribeAllResourceEvents>(_ =>
            {
                EventBus.Unsubscribe(Sender);
            });
        }

        /// <summary>
        ///     The underlying event bus.
        /// </summary>
        ResourceEventBus EventBus { get; } = new ResourceEventBus();

        /// <summary>
        ///     The <see cref="KubeApiClient"/> used to communicate with the Kubernetes API.
        /// </summary>
        KubeApiClient KubeClient { get; }

        /// <summary>
        ///     An <see cref="IObservable"/> that manages the underlying subscription to the Kubernetes API.
        /// </summary>
        IObservable<ResourceEventV1<ServiceV1>> EventSource { get; }

        /// <summary>
        ///     Called when the actor is started.
        /// </summary>
        protected override void PreStart()
        {
            IActorRef self = Self;
            
            _eventSourceSubscription = EventSource.Subscribe(
                onNext: resourceEvent => self.Tell(resourceEvent),
                onError: error => Log.Error(error, "Error reported by event source: {ErrorMessage}", error.Message),
                onCompleted: () =>
                {
                    Log.Info("Event source has shut down; actor will terminate.");

                    self.Tell(PoisonPill.Instance);
                }
            );
        }

        /// <summary>
        ///     Called when the actor is stopped.
        /// </summary>
        protected override void PostStop()
        {
            if (_eventSourceSubscription != null)
            {
                _eventSourceSubscription.Dispose();
                _eventSourceSubscription = null;
            }
        }

        /// <summary>
        ///     The underlying event bus.
        /// </summary>
        class ResourceEventBus
            : ResourceEventBus<ServiceV1, ResourceEventFilter>
        {
            /// <summary>
            ///     Get the metadata for the specified resource.
            /// </summary>
            /// <param name="replicationController">
            ///     A <see cref="ServiceV1"/> representing the target Service.
            /// </param>
            /// <returns>
            ///     The resource metadata.
            /// </returns>
            protected override ObjectMetaV1 GetMetadata(ServiceV1 replicationController)
            {
                if (replicationController == null)
                    throw new ArgumentNullException(nameof(replicationController));
                
                return replicationController.Metadata;
            }

            /// <summary>
            ///     Create a filter that exactly matches the specified Service metadata.
            /// </summary>
            /// <param name="metadata">
            ///     The Service metadata to match.
            /// </param>
            /// <returns>
            ///     A <see cref="ResourceEventFilter"/> describing the filter.
            /// </returns>
            protected override ResourceEventFilter CreateExactMatchFilter(ObjectMetaV1 metadata) => ResourceEventFilter.FromMetatadata(metadata);
        }
    }
}
