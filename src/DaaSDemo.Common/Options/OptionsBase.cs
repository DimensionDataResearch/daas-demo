using Microsoft.Extensions.Configuration;
using System;

namespace DaaSDemo.Common.Options
{
    /// <summary>
    ///     The base class for options.
    /// </summary>
    public abstract class OptionsBase
    {
        /// <summary>
        ///     Load <typeparamref name="TOptions"/> from configuration.
        /// </summary>
        /// <typeparam name="TOptions">
        ///     The type of options to load.
        /// </typeparam>
        /// <param name="configuration">
        ///     The <see cref="IConfiguration"/>.
        /// </param>
        /// <param name="key">
        ///     An optional sub-key name to load from.
        /// </param>
        /// <returns>
        ///     The <typeparamref name="TOptions"/>.
        /// </returns>
        protected static TOptions Load<TOptions>(IConfiguration configuration, string key)
            where TOptions : OptionsBase, new()
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));
            
            TOptions options = new TOptions();
            if (!String.IsNullOrWhiteSpace(key))
                ConfigurationBinder.Bind(configuration, key, options);
            else
                ConfigurationBinder.Bind(configuration, options);

            return options;
        }
    }
}
