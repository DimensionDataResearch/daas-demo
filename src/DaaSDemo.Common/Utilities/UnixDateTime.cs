using System;

namespace DaaSDemo.Common.Utilities
{
    /// <summary>
    ///     Helper methods for working with Unix-style dates and times.
    /// </summary>
    public static class UnixDateTime
    {
        /// <summary>
        ///     The Unix epoch (01/01/1970).
        /// </summary>
        static readonly DateTime Epoch = new DateTime(1970, 1, 1);

        /// <summary>
        ///     Convert a Unix timestamp to a <see cref="DateTime"/>.
        /// </summary>
        /// <param name="unixTimestamp">
        ///     The Unix-style timestamp to convert.
        /// </param>
        /// <returns>
        ///     The converted <see cref="DateTime"/>
        /// </returns>
        public static DateTime FromUnix(long unixTimestamp) => Epoch.AddTicks(unixTimestamp);

        /// <summary>
        ///     Convert a <see cref="DateTime"/> to a Unix-style timestamp.
        /// </summary>
        /// <param name="dateTime">
        ///     The <see cref="DateTime"/> to convert.
        /// </param>
        /// <returns>
        ///     The converted Unix-style timestamp.
        /// </returns>
        public static long ToUnix(DateTime dateTime)
        {
            if (dateTime < Epoch)
                throw new ArgumentOutOfRangeException(nameof(dateTime), dateTime, "Cannot convert a date / time before the Unix epoch.");

            return (dateTime - Epoch).Ticks;
        }
    }
}
