using System;
using System.Collections.Generic;

namespace DaaSDemo.Common.Utilities
{
    /// <summary>
    ///     Extension methods for collection types.
    /// </summary>
    public static class CollectionExtensions
    {
        /// <summary>
        ///     Destructure a key / value pair into a key / value tuple.
        /// </summary>
        /// <typeparam name="TKey">
        ///     The key type.
        /// </typeparam>
        /// <typeparam name="TValue">
        ///     The value type.
        /// </typeparam>
        /// <param name="key">
        ///     The key.
        /// </param>
        /// <param name="value">
        ///     The value.
        /// </param>
        public static void Deconstruct<TKey, TValue>(this KeyValuePair<TKey, TValue> keyValuePair, out TKey key, out TValue value)
        {
            key = keyValuePair.Key;
            value = keyValuePair.Value;
        }
    }
}
