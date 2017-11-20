namespace DaaSDemo.KubeClient
{
    using System;
    using ResourceClients;

    /// <summary>
    ///     Extension methods for creating Kubernetes resource clients for a <see cref="KubeApiClient"/>.
    /// </summary>
    public static class ClientFactoryExtensions
    {
        /// <summary>
        ///     Get the Kubernetes ConfigMaps (v1) resource client.
        /// </summary>
        /// <param name="kubeClient">
        ///     The Kubernetes API client.
        /// </param>
        /// <returns>
        ///     The resource client.
        /// </returns>
        public static ConfigMapClientV1 ConfigMapsV1(this KubeApiClient kubeClient)
        {
            if (kubeClient == null)
                throw new ArgumentNullException(nameof(kubeClient));
            
            return kubeClient.ResourceClient(
                client => new ConfigMapClientV1(client)
            );
        }

        /// <summary>
        ///     Get the Kubernetes Secrets (v1) resource client.
        /// </summary>
        /// <param name="kubeClient">
        ///     The Kubernetes API client.
        /// </param>
        /// <returns>
        ///     The resource client.
        /// </returns>
        public static SecretClientV1 SecretsV1(this KubeApiClient kubeClient)
        {
            if (kubeClient == null)
                throw new ArgumentNullException(nameof(kubeClient));
            
            return kubeClient.ResourceClient(
                client => new SecretClientV1(client)
            );
        }

        /// <summary>
        ///     Get the Kubernetes PersistentVolumes (v1) resource client.
        /// </summary>
        /// <param name="kubeClient">
        ///     The Kubernetes API client.
        /// </param>
        /// <returns>
        ///     The resource client.
        /// </returns>
        public static PersistentVolumeClientV1 PersistentVolumesV1(this KubeApiClient kubeClient)
        {
            if (kubeClient == null)
                throw new ArgumentNullException(nameof(kubeClient));
            
            return kubeClient.ResourceClient(
                client => new PersistentVolumeClientV1(client)
            );
        }

        /// <summary>
        ///     Get the Kubernetes PersistentVolumeClaims (v1) resource client.
        /// </summary>
        /// <param name="kubeClient">
        ///     The Kubernetes API client.
        /// </param>
        /// <returns>
        ///     The resource client.
        /// </returns>
        public static PersistentVolumeClaimClientV1 PersistentVolumeClaimsV1(this KubeApiClient kubeClient)
        {
            if (kubeClient == null)
                throw new ArgumentNullException(nameof(kubeClient));
            
            return kubeClient.ResourceClient(
                client => new PersistentVolumeClaimClientV1(client)
            );
        }

        /// <summary>
        ///     Get the Kubernetes Pods (v1) resource client.
        /// </summary>
        /// <param name="kubeClient">
        ///     The Kubernetes API client.
        /// </param>
        /// <returns>
        ///     The resource client.
        /// </returns>
        public static PodClientV1 PodsV1(this KubeApiClient kubeClient)
        {
            if (kubeClient == null)
                throw new ArgumentNullException(nameof(kubeClient));
            
            return kubeClient.ResourceClient(
                client => new PodClientV1(client)
            );
        }

        /// <summary>
        ///     Get the Kubernetes Services (v1) resource client.
        /// </summary>
        /// <param name="kubeClient">
        ///     The Kubernetes API client.
        /// </param>
        /// <returns>
        ///     The resource client.
        /// </returns>
        public static ServiceClientV1 ServicesV1(this KubeApiClient kubeClient)
        {
            if (kubeClient == null)
                throw new ArgumentNullException(nameof(kubeClient));
            
            return kubeClient.ResourceClient(
                client => new ServiceClientV1(client)
            );
        }

        /// <summary>
        ///     Get the Kubernetes ReplicationControllers (v1) resource client.
        /// </summary>
        /// <param name="kubeClient">
        ///     The Kubernetes API client.
        /// </param>
        /// <returns>
        ///     The resource client.
        /// </returns>
        public static ReplicationControllerClientV1 ReplicationControllersV1(this KubeApiClient kubeClient)
        {
            if (kubeClient == null)
                throw new ArgumentNullException(nameof(kubeClient));
            
            return kubeClient.ResourceClient(
                client => new ReplicationControllerClientV1(client)
            );
        }

        /// <summary>
        ///     Get the Kubernetes Jobs (v1) resource client.
        /// </summary>
        /// <param name="kubeClient">
        ///     The Kubernetes API client.
        /// </param>
        /// <returns>
        ///     The resource client.
        /// </returns>
        public static JobClientV1 JobsV1(this KubeApiClient kubeClient)
        {
            if (kubeClient == null)
                throw new ArgumentNullException(nameof(kubeClient));
            
            return kubeClient.ResourceClient(
                client => new JobClientV1(client)
            );
        }

        /// <summary>
        ///     Get the Kubernetes Deployments (v1beta1) resource client.
        /// </summary>
        /// <param name="kubeClient">
        ///     The Kubernetes API client.
        /// </param>
        /// <returns>
        ///     The resource client.
        /// </returns>
        public static DeploymentClientV1Beta1 DeploymentsV1Beta1(this KubeApiClient kubeClient)
        {
            if (kubeClient == null)
                throw new ArgumentNullException(nameof(kubeClient));
            
            return kubeClient.ResourceClient(
                client => new DeploymentClientV1Beta1(client)
            );
        }

        /// <summary>
        ///     Get the Kubernetes PrometheusServiceMonitors (v1) resource client.
        /// </summary>
        /// <param name="kubeClient">
        ///     The Kubernetes API client.
        /// </param>
        /// <returns>
        ///     The resource client.
        /// </returns>
        public static PrometheusServiceMonitorClientV1 PrometheusServiceMonitorsV1(this KubeApiClient kubeClient)
        {
            if (kubeClient == null)
                throw new ArgumentNullException(nameof(kubeClient));
            
            return kubeClient.ResourceClient(
                client => new PrometheusServiceMonitorClientV1(client)
            );
        }
    }
}
