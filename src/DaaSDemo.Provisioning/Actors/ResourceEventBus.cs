using Akka.Actor;
using Akka.Event;
using System;

namespace DaaSDemo.Provisioning.Actors
{
    using Filters;
    using KubeClient.Models;

    // TODO: Custom message types for the various event types.
    // TODO: Consider adding support for resource namespacing.
    // TODO: Consider adding support for filtering by Kubernetes labels.

    /// <summary>
    ///		Event bus for Kubernetes resource events.
    /// </summary>
    /// <typeparam name="TResource">
    ///     The type of resource that events relate to.
    /// </typeparam>
    /// <typeparam name="TFilter">
    ///     The type used to describe event filters.
    /// </typeparam>
    public abstract class ResourceEventBus<TResource, TFilter>
        : ActorEventBus<ResourceEventV1<TResource>, TFilter>
            where TResource : KubeResourceV1
            where TFilter : EventFilter
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
        protected abstract ObjectMetaV1 GetMetadata(TResource resource);

        /// <summary>
        ///     Create a filter that exactly matches the specified resource metadata.
        /// </summary>
        /// <param name="metadata">
        ///     The resource metadata to match.
        /// </param>
        /// <returns>
        ///     A <typeparamref name="TFilter"/> describing the filter.
        /// </returns>
        protected abstract TFilter CreateExactMatchFilter(ObjectMetaV1 metadata);

        /// <summary>
        ///		Determine whether <paramref name="child"/> is a sub-classification of <paramref name="parent"/>.
        /// </summary>
        /// <param name="parent">
        ///		The parent classifier.
        /// </param>
        /// <param name="child">
        ///		The child classifier.
        /// </param>
        /// <returns>
        ///		<c>true</c>, if <paramref name="child"/> equals <paramref name="parent"/> / <paramref name="child"/> is empty; otherwise, <c>false</c>.
        /// </returns>
        protected override bool IsSubClassification(TFilter parent, TFilter child) => false;

        /// <summary>
        ///		Get a classifier for the specified <see cref="JobStore"/> event.
        /// </summary>
        /// <param name="resourceEvent">
        ///		The <typeparam name="TResource"/> event.
        /// </param>
        /// <returns>
        ///		The event classifier.
        /// </returns>
        protected sealed override TFilter GetClassifier(ResourceEventV1<TResource> resourceEvent)
        {
            if (resourceEvent == null)
                throw new ArgumentNullException(nameof(resourceEvent));

            return CreateExactMatchFilter(
                GetMetadata(resourceEvent.Resource)
            );
        }

        /// <summary>
        ///		Determine whether the specified <see cref="JobStore"/> event matches the specified clasifier.
        /// </summary>
        /// <param name="resourceEvent">
        ///		The <see cref="JobStore"/> event.
        /// </param>
        /// <param name="resourceFilter">
        ///		The event classifier.
        /// </param>
        /// <returns>
        ///		<c>true</c>, if the event matches the filter; otherwise, <c>false</c>.
        /// </returns>
        protected override bool Classify(ResourceEventV1<TResource> resourceEvent, TFilter resourceFilter)
        {
            if (resourceEvent == null)
                throw new ArgumentNullException(nameof(resourceEvent));

            if (resourceFilter == null)
                throw new ArgumentNullException(nameof(resourceFilter));

            return resourceFilter.IsMatch(
                GetMetadata(resourceEvent.Resource)
            );
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
        protected override void Publish(ResourceEventV1<TResource> resourceEvent, IActorRef subscriber)
        {
            if (resourceEvent == null)
                throw new ArgumentNullException(nameof(resourceEvent));

            subscriber.Tell(resourceEvent);
        }
    }
}
