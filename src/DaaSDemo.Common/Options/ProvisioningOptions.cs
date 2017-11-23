using System;
using Microsoft.Extensions.Configuration;

namespace DaaSDemo.Common.Options
{
    /// <summary>
    ///     Provisioning-related options for the DaaS API.
    /// </summary>
    public class ProvisioningOptions
        : OptionsBase
    {
        /// <summary>
        ///     Images by the provisioning engine.
        /// </summary>
        public ProvisioningImageOptions Images { get; set; } = new ProvisioningImageOptions();

        /// <summary>
        ///     Load <see cref="ProvisioningOptions"/> from configuration.
        /// </summary>
        /// <param name="configuration">
        ///     The <see cref="IConfiguration"/>.
        /// </param>
        /// <param name="key">
        ///     An optional sub-key name to load from.
        /// </param>
        /// <returns>
        ///     The <see cref="ProvisioningOptions"/>.
        /// </returns>
        public static ProvisioningOptions From(IConfiguration configuration, string key = "Provisioning") => Load<ProvisioningOptions>(configuration, key);
    }

    /// <summary>
    ///     Images used by the provisioning engine.
    /// </summary>
    public class ProvisioningImageOptions
    {
        /// <summary>
        ///     The SQL Server image name and tag.
        /// </summary>
        public string SQL { get; set; }

        /// <summary>
        ///     The Prometheus Exporter for SQL Server image name and tag.
        /// </summary>
        public string SQLExporter { get; set; }

        /// <summary>
        ///     The RavenDB image name and tag.
        /// </summary>
        public string RavenDB { get; set; }
    }
}
