using System;
using Microsoft.Extensions.Configuration;

namespace DaaSDemo.Common.Options
{
    /// <summary>
    ///     Kubernetes-related options for the DaaS API.
    /// </summary>
    public class KubernetesOptions
        : OptionsBase
    {
        /// <summary>
        ///     The Kubernetes namespace in which DaaS components are hosted.
        /// </summary>
        public string KubeNamespace { get; set; }

        /// <summary>
        ///     Load <see cref="KubernetesOptions"/> from configuration.
        /// </summary>
        /// <param name="configuration">
        ///     The <see cref="IConfiguration"/>.
        /// </param>
        /// <param name="key">
        ///     An optional sub-key name to load from.
        /// </param>
        /// <returns>
        ///     The <see cref="KubernetesOptions"/>.
        /// </returns>
        public static KubernetesOptions From(IConfiguration configuration, string key = "Kubernetes") => Load<KubernetesOptions>(configuration, key);
    }
}
