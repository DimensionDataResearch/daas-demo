using System;

namespace DaaSDemo.Provisioning.Messages
{
    using Filters;

    /// <summary>
    ///     Unsubscribe from resource events.
    /// </summary>
    /// <typeparam name="TFilter">
    ///     The type that describes event filters.
    /// </typeparam>
    public class UnsubscribeResourceEvents<TFilter>
        where TFilter : EventFilter
    {
        /// <summary>
        ///     Create a new <see cref="UnsubscribeResourceEvents"/> message.
        /// </summary>
        /// <param name="filter">
        ///     A <typeparamref name="TFilter"/> representing the filter for events.
        /// </param>
        public UnsubscribeResourceEvents(TFilter filter)
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
    ///     Factory for <see cref="UnsubscribeResourceEvents{TFilter}"/> messages.
    /// </summary>
    public static class UnsubscribeResourceEvents
    {
        /// <summary>
        ///     Create a new <see cref="UnsubscribeResourceEvents{TFilter}"/> message.
        /// </summary>
        /// <typeparam name="TFilter">
        ///     The type that describes event filters.
        /// </typeparam>
        /// <param name="filter">
        ///     A <typeparamref name="TFilter"/> representing the filter for events.
        /// </param>
        /// <returns>
        ///     The new <see cref="UnsubscribeResourceEvents{TFilter}"/> message.
        /// </returns>
        public static UnsubscribeResourceEvents<TFilter> Create<TFilter>(TFilter filter)
            where TFilter : EventFilter
        {
            return new UnsubscribeResourceEvents<TFilter>(filter);
        }
    }
}
