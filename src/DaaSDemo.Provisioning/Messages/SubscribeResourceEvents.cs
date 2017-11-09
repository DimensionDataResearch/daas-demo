using System;

namespace DaaSDemo.Provisioning.Messages
{
    using Filters;

    /// <summary>
    ///     Subscribe to resource events.
    /// </summary>
    /// <typeparam name="TFilter">
    ///     The type that describes event filters.
    /// </typeparam>
    public class SubscribeResourceEvents<TFilter>
        where TFilter : EventFilter
    {
        /// <summary>
        ///     Create a new <see cref="SubscribeResourceEvents"/> message.
        /// </summary>
        /// <param name="filter">
        ///     A <typeparamref name="TFilter"/> representing the filter for events.
        /// </param>
        public SubscribeResourceEvents(TFilter filter)
        {
            if (filter == null)
                throw new ArgumentNullException(nameof(filter));

            Filter = filter;            
        }

        /// <summary>
        ///     A <typeparamref name="TFilter"/> representing the filter for events.
        /// </summary>
        public TFilter Filter { get; }
    }

    /// <summary>
    ///     Factory for <see cref="SubscribeResourceEvents{TFilter}"/> messages.
    /// </summary>
    public static class SubscribeResourceEvents
    {
        /// <summary>
        ///     Create a new <see cref="SubscribeResourceEvents{TFilter}"/> message.
        /// </summary>
        /// <typeparam name="TFilter">
        ///     The type that describes event filters.
        /// </typeparam>
        /// <param name="filter">
        ///     A <typeparamref name="TFilter"/> representing the filter for events.
        /// </param>
        /// <returns>
        ///     The new <see cref="SubscribeResourceEvents{TFilter}"/> message.
        /// </returns>
        public static SubscribeResourceEvents<TFilter> Create<TFilter>(TFilter filter)
            where TFilter : EventFilter
        {
            return new SubscribeResourceEvents<TFilter>(filter);
        }
    }
}
