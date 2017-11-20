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
        ///     The name of the Kubernetes pod (if any) where the application is running.
        /// </summary>
        public string PodName { get; set; }

        /// <summary>
        ///     The fully-qualified (public) domain name of the Kubernetes cluster.
        /// </summary>
        public string ClusterPublicFQDN { get; set; }

        /// <summary>
        ///     The name of the volume claim representing the volume used to store tenant databases.
        /// </summary>
        public string VolumeClaimName { get; set; }

        /// <summary>
        ///     The base address of the Kubernetes API end-point.
        /// </summary>
        public string ApiEndPoint { get; set; }

        /// <summary>
        ///     The access token for the Kubernetes API end-point.
        /// </summary>
        public string Token { get; }

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
