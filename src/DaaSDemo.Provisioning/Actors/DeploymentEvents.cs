using Akka.Actor;
using KubeNET.Swagger.Model;
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
    ///     Actor that publishes events relating to Kubernetes Deployments in the default namespace.
    /// </summary>
    public class DeploymentEvents
        : ReceiveActorEx
    {
        /// <summary>
        ///     An <see cref="IDisposable"/> representing the underlying subscription to the Kubernetes API.
        /// </summary>
        IDisposable _eventSourceSubscription;

        /// <summary>
        ///     Create a new <see cref="DeploymentEvents"/> actor.
        /// </summary>
        /// <param name="kubeClient">
        ///     The <see cref="KubeApiClient"/> used to communicate with the Kubernetes API.
        /// </param>
        public DeploymentEvents(KubeApiClient kubeClient)
        {
            if (kubeClient == null)
                throw new ArgumentNullException(nameof(kubeClient));
            
            KubeClient = kubeClient;
            EventSource = KubeClient.DeploymentsV1Beta1.WatchAll(
                kubeNamespace: "default"
            );

            Receive<ResourceEventV1<V1beta1Deployment>>(resourceEvent =>
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
        IObservable<ResourceEventV1<V1beta1Deployment>> EventSource { get; }

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
            : ResourceEventBus<V1beta1Deployment, ResourceEventFilter>
        {
            /// <summary>
            ///     Get the metadata for the specified resource.
            /// </summary>
            /// <param name="replicationController">
            ///     A <see cref="V1beta1Deployment"/> representing the target Deployment.
            /// </param>
            /// <returns>
            ///     The resource metadata.
            /// </returns>
            protected override V1ObjectMeta GetMetadata(V1beta1Deployment replicationController)
            {
                if (replicationController == null)
                    throw new ArgumentNullException(nameof(replicationController));
                
                return replicationController.Metadata;
            }

            /// <summary>
            ///     Create a filter that exactly matches the specified Deployment metadata.
            /// </summary>
            /// <param name="metadata">
            ///     The Deployment metadata to match.
            /// </param>
            /// <returns>
            ///     A <see cref="ResourceEventFilter"/> describing the filter.
            /// </returns>
            protected override ResourceEventFilter CreateExactMatchFilter(V1ObjectMeta metadata) => ResourceEventFilter.FromMetatadata(metadata);
        }
    }
}
