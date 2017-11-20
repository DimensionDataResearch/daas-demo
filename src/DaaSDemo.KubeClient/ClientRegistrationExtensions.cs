using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DaaSDemo.KubeClient
{
    using Common.Options;

    /// <summary>
    ///     Extension methods for registering <see cref="KubeApiClient"/> as a component.
    /// </summary>
    public static class ClientRegistrationExtensions
    {
        /// <summary>
        ///     Is the application running in Kubernetes?
        /// </summary>
        static bool IsKubernetes => Environment.GetEnvironmentVariable("IN_KUBERNETES") == "1";

        /// <summary>
        ///     Add a <see cref="KubeApiClient"/> to the service collection.
        /// </summary>
        /// <param name="services">
        ///     The service collection.
        /// </param>
        public static void AddKubeClient(this IServiceCollection services)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));
            
            if (IsKubernetes)
            {
                // When running inside Kubernetes, use pod-level service account (e.g. access token from mounted Secret).
                services.AddScoped<KubeClient.KubeApiClient>(
                    serviceProvider => KubeClient.KubeApiClient.CreateFromPodServiceAccount()
                );
            }
            else
            {
                // For debugging purposes only.
                services.AddScoped<KubeClient.KubeApiClient>(serviceProvider =>
                {
                    KubernetesOptions kubeOptions = serviceProvider.GetRequiredService<IOptions<KubernetesOptions>>().Value;

                    if (String.IsNullOrWhiteSpace(kubeOptions.ApiEndPoint))
                    throw new InvalidOperationException("Application configuration is missing Kubernetes API end-point.");

                    if (String.IsNullOrWhiteSpace(kubeOptions.Token))
                    throw new InvalidOperationException("Application configuration is missing Kubernetes API token.");

                    return KubeClient.KubeApiClient.Create(
                        endPointUri: new Uri(kubeOptions.ApiEndPoint),
                        accessToken: kubeOptions.Token
                    );
                });
            }
        }
    }
}
