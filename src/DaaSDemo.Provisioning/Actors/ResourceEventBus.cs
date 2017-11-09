using Akka.Actor;
using Akka.Event;
using KubeNET.Swagger.Model;
using System;

namespace DaaSDemo.Provisioning.Actors
{
    using KubeClient.Models;

    // TODO: Custom message types for the various event types.
    // TODO: Consider adding support for resource namespacing.
    // TODO: Consider adding support for filtering by Kubernetes labels.

    /// <summary>
    ///		Event bus for Kubernetes resource events.
    /// </summary>
    public abstract class ResourceEventBus<TResource>
        : ActorEventBus<V1ResourceEvent<TResource>, string>
    {
        /// <summary>
        ///		Create a new <see cref="ResourceEventBus"/>.
        /// </summary>
        public ResourceEventBus()
        {
        }

        /// <summary>
        ///     Get the metadata for the specified resource.
        /// </summary>
        /// <param name="resource">
        ///     The target resource.
        /// </param>
        /// <returns>
        ///     The resource metadata.
        /// </returns>
        protected abstract V1ObjectMeta GetMetadata(TResource resource);

        /// <summary>
        ///		Determine whether <paramref name="resourceName"/> is a sub-classification of <paramref name="resourceNameFilter"/>.
        /// </summary>
        /// <param name="resourceNameFilter">
        ///		The parent classifier.
        /// </param>
        /// <param name="resourceName">
        ///		The child classifier.
        /// </param>
        /// <returns>
        ///		<c>true</c>, if <paramref name="resourceName"/> equals <paramref name="resourceNameFilter"/> / <paramref name="resourceName"/> is empty; otherwise, <c>false</c>.
        /// </returns>
        protected sealed override bool IsSubClassification(string resourceNameFilter, string resourceName)
        {
            return resourceName == resourceNameFilter || resourceName == String.Empty; // String.Empty means match any resource.
        }

        /// <summary>
        ///		Get a classifier for the specified <see cref="JobStore"/> event.
        /// </summary>
        /// <param name="resourceEvent">
        ///		The <typeparam name="TResource"/> event.
        /// </param>
        /// <returns>
        ///		The event classifier.
        /// </returns>
        protected sealed override string GetClassifier(V1ResourceEvent<TResource> resourceEvent)
        {
            if (resourceEvent == null)
                throw new ArgumentNullException(nameof(resourceEvent));

            return GetMetadata(resourceEvent.Resource).Name;
        }

        /// <summary>
        ///		Determine whether the specified <see cref="JobStore"/> event matches the specified clasifier.
        /// </summary>
        /// <param name="resourceEvent">
        ///		The <see cref="JobStore"/> event.
        /// </param>
        /// <param name="resourceNameFilter">
        ///		The event classifier.
        /// </param>
        /// <returns>
        ///		<c>true</c>, if <paramref name="resourceName"/> equals <paramref name="resourceNameFilter"/> / <paramref name="resourceName"/> is empty; otherwise, <c>false</c>.
        /// </returns>
        protected override bool Classify(V1ResourceEvent<TResource> resourceEvent, string resourceNameFilter)
        {
            if (resourceEvent == null)
                throw new ArgumentNullException(nameof(resourceEvent));

            if (resourceNameFilter == null)
                throw new ArgumentNullException(nameof(resourceNameFilter));

            return GetMetadata(resourceEvent.Resource).Name == resourceNameFilter;
        }

        /// <summary>
        ///		Publish an event on the bus.
        /// </summary>
        /// <param name="resourceEvent">
        ///		The job store event.
        /// </param>
        /// <param name="subscriber">
        ///		An <see cref="IActorRef"/> representing the actor to subscribe.
        /// </param>
        protected override void Publish(V1ResourceEvent<TResource> resourceEvent, IActorRef subscriber)
        {
            if (resourceEvent == null)
                throw new ArgumentNullException(nameof(resourceEvent));

            subscriber.Tell(resourceEvent);
        }
    }
}
