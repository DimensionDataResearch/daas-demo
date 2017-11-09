using Akka.Actor;
using KubeNET.Swagger.Model;
using System;

namespace DaaSDemo.Provisioning.Actors
{
    using KubeClient;
    using KubeClient.Models;
    using Messages;

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
            EventSource = KubeClient.ServicesV1.WatchAll(
                kubeNamespace: "default"
            );

            Receive<V1ResourceEvent<V1Service>>(resourceEvent =>
            {
                EventBus.Publish(resourceEvent);
            });
            Receive<SubscribeResourceEvents>(subscribe =>
            {
                EventBus.Subscribe(Sender, subscribe.ResourceName);
            });
            Receive<UnsubscribeResourceEvents>(unsubscribe =>
            {
                EventBus.Unsubscribe(Sender, unsubscribe.ResourceName);
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
        IObservable<V1ResourceEvent<V1Service>> EventSource { get; }

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
            : ResourceEventBus<V1Service>
        {
            /// <summary>
            ///     Get the metadata for the specified resource.
            /// </summary>
            /// <param name="resource">
            ///     The target resource.
            /// </param>
            /// <returns>
            ///     The resource metadata.
            /// </returns>
            protected override V1ObjectMeta GetMetadata(V1Service resource)
            {
                if (resource == null)
                    throw new ArgumentNullException(nameof(resource));
                
                return resource.Metadata;
            }
        }
    }
}
