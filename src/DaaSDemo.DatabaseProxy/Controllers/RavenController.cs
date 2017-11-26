using HTTPlease;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using Raven.Client.Documents.Session;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DaaSDemo.DatabaseProxy.Controllers
{
    using Common.Options;
    using Data;
    using Models.Data;
    using KubeClient;
    using KubeClient.Models;
    using Models.DatabaseProxy;
    using System.Net.Http;
    using System.Threading;

    /// <summary>
    ///     Controller for the RavenDB proxy API.
    /// </summary>
    [Route("api/v1/raven")]
    public class RavenController
        : Controller
    {
        /// <summary>
        ///     The database Id representing the "master" database in any RavenDB server.
        /// </summary>
        const string MasterDatabaseId = "master";

        /// <summary>
        ///     Create a new <see cref="RavenController"/>.
        /// </summary>
        /// <param name="documentSession">
        ///     The RavenDB document session for the current request.
        /// </param>
        /// <param name="kubeClient">
        ///     The Kubernetes API client.
        /// </param>
        /// <param name="kubeOptions">
        ///     The application's Kubernetes options.
        /// </param>
        /// <param name="logger">
        ///     The controller logger.
        /// </param>
        public RavenController(IAsyncDocumentSession documentSession, KubeApiClient kubeClient, IOptions<KubernetesOptions> kubeOptions, ILogger<RavenController> logger)
        {
            if (documentSession == null)
                throw new ArgumentNullException(nameof(documentSession));

            if (kubeClient == null)
                throw new ArgumentNullException(nameof(kubeClient));

            if (kubeOptions == null)
                throw new ArgumentNullException(nameof(kubeOptions));

            if (logger == null)
                throw new ArgumentNullException(nameof(logger));
            
            DocumentSession = documentSession;
            KubeClient = kubeClient;
            KubeOptions = kubeOptions.Value;
            Log = logger;
        }

        /// <summary>
        ///     The RavenDB document session for the current request.
        /// </summary>
        IAsyncDocumentSession DocumentSession { get; }

        /// <summary>
        ///     The Kubernetes API client.
        /// </summary>
        KubeApiClient KubeClient { get; }

        /// <summary>
        ///     The application's Kubernetes options.
        /// </summary>
        KubernetesOptions KubeOptions { get; }

        /// <summary>
        ///     The controller logger.
        /// </summary>
        ILogger Log { get; }

        [HttpPost("{serverId}/initialize")]
        async Task<IActionResult> InitializeServerConfiguration(string serverId)
        {
            if (String.IsNullOrWhiteSpace(serverId))
                throw new ArgumentException("Argument cannot be null, empty, or entirely composed of whitespace: 'serverId'.", nameof(serverId));

            Log.LogInformation("Initialising configuration for server {ServerId}...", serverId);

            // TODO: Use shared instance of HttpClient.
            using (HttpClient client = new HttpClient())
            using (CancellationTokenSource cancellationSource = new CancellationTokenSource())
            {
                client.BaseAddress = await GetServerBaseAddress(serverId);
                if (client.BaseAddress == null)
                    return NotFound($"Cannot determine base address for server '{serverId}'.");

                cancellationSource.CancelAfter(
                    TimeSpan.FromSeconds(30)
                );
                
                Log.LogInformation("Using (deliberately) insecure mode for server {ServerId}...", serverId);

                HttpResponseMessage response = await client.PostAsJsonAsync(Requests.StartUnsecuredSetup,
                    postBody: new
                    {
                        PublicServerUrl = client.BaseAddress.AbsoluteUri,
                        Port = 8080,
                        Addresses = new string[]
                        {
                            "127.0.0.1"
                        }
                    },
                    cancellationToken: cancellationSource.Token
                );
                using (response)
                {
                    response.EnsureSuccessStatusCode();
                }

                Log.LogInformation("Committing changes to configuration for server {ServerId}...", serverId);

                response = await client.PostAsync(Requests.CompleteSetup,
                    cancellationToken: cancellationSource.Token
                );
                using (response)
                {
                    response.EnsureSuccessStatusCode();
                }
                
                while (true)
                {
                    using (CancellationTokenSource aliveCheckCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationSource.Token))
                    {
                        aliveCheckCancellationSource.CancelAfter(
                            TimeSpan.FromSeconds(1)
                        );

                        try
                        {
                            Log.LogDebug("Checking to see if server {ServerId} has restarted...", serverId);

                            response = await client.PostAsync(Requests.IsServerAlive,
                                cancellationToken: cancellationSource.Token
                            );
                            using (response)
                            {
                                response.EnsureSuccessStatusCode();

                                break;
                            }
                        }
                        catch (OperationCanceledException timedOut)
                        {
                            if (timedOut.CancellationToken == cancellationSource.Token)
                                throw new TimeoutException("Timed out after waiting 30 seconds for RavenDB server to restart.", timedOut);
                        }
                    }
                }

                Log.LogInformation("Configuration has been initialised for server {ServerId}.", serverId);

                return Ok("Server configuration initialised.");
            }
        }

        /// <summary>
        ///     Determine the connection string for the specified server.
        /// </summary>
        /// <param name="serverId">
        ///     The Id of the target server.
        /// </param>
        /// <returns>
        ///     The base URL, or <c>null</c> if the connection string could not be determined.
        /// </returns>
        async Task<Uri> GetServerBaseAddress(string serverId)
        {
            if (serverId == null)
                throw new ArgumentNullException(nameof(serverId));

            Log.LogInformation("Determining connection string for server {ServerId}...",
                serverId
            );

            DatabaseServer targetServer = await DocumentSession.LoadAsync<DatabaseServer>(serverId);
            if (targetServer == null)
            {
                Log.LogWarning("Cannot determine connection string for server {ServerId} (server not found).",
                    serverId
                );

                return null;
            }

            List<ServiceV1> matchingServices = await KubeClient.ServicesV1().List(
                labelSelector: $"cloud.dimensiondata.daas.server-id = {targetServer.Id},cloud.dimensiondata.daas.service-type = internal",
                kubeNamespace: KubeOptions.KubeNamespace
            );
            if (matchingServices.Count == 0)
            {
                Log.LogWarning("Cannot determine connection string for server {ServerId} (server's associated Kubernetes Service not found).",
                    serverId
                );

                return null;
            }

            ServiceV1 serverService = matchingServices[matchingServices.Count - 1];
            string serverFQDN = $"{serverService.Metadata.Name}.{serverService.Metadata.Namespace}.svc.cluster.local";
            int serverPort = serverService.Spec.Ports[0].Port;

            Log.LogInformation("Database proxy will connect to RavenDB server '{ServerFQDN}' on {ServerPort}.", serverFQDN, serverPort);

            return new Uri($"http://{serverFQDN}:{serverPort}");
        }

        public static class Requests
        {
            public static readonly HttpRequest StartUnsecuredSetup = HttpRequest.Factory.Json("setup/unsecured");

            public static readonly HttpRequest CompleteSetup = HttpRequest.Factory.Json("setup/finish");

            public static readonly HttpRequest IsServerAlive = HttpRequest.Factory.Json("setup/alive");
        }
    }
}
