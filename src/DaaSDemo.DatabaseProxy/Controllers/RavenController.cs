using HTTPlease;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using Raven.Client.Documents;
using Raven.Client.Documents.Session;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace DaaSDemo.DatabaseProxy.Controllers
{
    using Common.Options;
    using Data;
    using Models.Data;
    using KubeClient;
    using KubeClient.Models;
    using Models.DatabaseProxy;

    /// <summary>
    ///     Controller for the RavenDB proxy API.
    /// </summary>
    [Route("api/v1/raven")]
    public class RavenController
        : DatabaseProxyController
    {
        /// <summary>
        ///     The database Id representing the "master" database in any RavenDB server.
        /// </summary>
        const string MasterDatabaseId = "master";

        /// <summary>
        ///     Create a new <see cref="RavenController"/>.
        /// </summary>
        /// <param name="documentSession">
        ///     The management database document session for the current request.
        /// </param>
        /// <param name="kubeClient">
        ///     The Kubernetes API client.
        /// </param>
        /// <param name="httpClient">
        ///     The HTTP client used to communicate directly with RavenDB instances.
        /// </param>
        /// <param name="kubeOptions">
        ///     The application's Kubernetes options.
        /// </param>
        /// <param name="logger">
        ///     The controller logger.
        /// </param>
        public RavenController(IAsyncDocumentSession documentSession, KubeApiClient kubeClient, HttpClient httpClient, IOptions<KubernetesOptions> kubeOptions, ILogger<RavenController> logger)
        {
            if (documentSession == null)
                throw new ArgumentNullException(nameof(documentSession));

            if (kubeClient == null)
                throw new ArgumentNullException(nameof(kubeClient));

            if (httpClient == null)
                throw new ArgumentNullException(nameof(httpClient));

            if (kubeOptions == null)
                throw new ArgumentNullException(nameof(kubeOptions));

            if (logger == null)
                throw new ArgumentNullException(nameof(logger));
            
            DocumentSession = documentSession;
            KubeClient = kubeClient;
            HttpClient = httpClient;
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
        ///     The HTTP client used to communicate directly with RavenDB instances.
        /// </summary>
        HttpClient HttpClient { get; }

        /// <summary>
        ///     The application's Kubernetes options.
        /// </summary>
        KubernetesOptions KubeOptions { get; }

        /// <summary>
        ///     The controller logger.
        /// </summary>
        ILogger Log { get; }

        /// <summary>
        ///     Initialise configuration for a RavenDB server.
        /// </summary>
        /// <param name="serverId">
        ///     The Id of the target server.
        /// </param>
        [HttpPost("{serverId}/initialize")]
        public async Task<IActionResult> InitializeServerConfiguration(string serverId)
        {
            if (String.IsNullOrWhiteSpace(serverId))
                throw new ArgumentException("Argument cannot be null, empty, or entirely composed of whitespace: 'serverId'.", nameof(serverId));

            Log.LogInformation("Initialising configuration for server {ServerId}...", serverId);
            
            DatabaseServer targetServer = await DocumentSession.LoadAsync<DatabaseServer>(serverId);

            using (CancellationTokenSource cancellationSource = new CancellationTokenSource())
            {
                Uri baseAddress = await GetServerBaseAddress(serverId);

                cancellationSource.CancelAfter(
                    TimeSpan.FromSeconds(30)
                );
                
                Log.LogInformation("Using (deliberately) insecure setup mode for server {ServerId}...", serverId);

                HttpResponseMessage response = await HttpClient.PostAsJsonAsync(
                    Requests.StartUnsecuredSetup.WithBaseUri(baseAddress),
                    postBody: new
                    {
                        PublicServerUrl = baseAddress.AbsoluteUri,
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

                response = await HttpClient.PostAsync(
                    Requests.CompleteSetup.WithBaseUri(baseAddress),
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

                            response = await HttpClient.GetAsync(
                                Requests.IsServerAlive.WithBaseUri(baseAddress),
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
        ///     Get the names of all databases present on the specified server.
        /// </summary>
        /// <param name="serverId">
        ///     The Id of the target server.
        /// </param>
        [HttpGet("{serverId}/database-names")]
        public async Task<IActionResult> GetServerDatabaseNames(string serverId)
        {
            if (String.IsNullOrWhiteSpace(serverId))
                throw new ArgumentException("Argument cannot be null, empty, or entirely composed of whitespace: 'serverId'.", nameof(serverId));

            using (IDocumentStore documentStore = await CreateDocumentStore(serverId))
            {
                if (documentStore == null)
                    return NotFound($"Cannot determine base address for server '{serverId}'.");

                using (CancellationTokenSource cancellationSource = new CancellationTokenSource())
                {
                    cancellationSource.CancelAfter(
                        TimeSpan.FromSeconds(30)
                    );

                    return Ok(
                        await documentStore.Admin.Server.GetDatabaseNames(0, 100, cancellationSource.Token)
                    );
                }
            }
        }

        /// <summary>
        ///     Create an <see cref="IDocumentStore"/> for the specified server.
        /// </summary>
        /// <param name="serverId">
        ///     The Id of the target server.
        /// </param>
        /// <returns>
        ///     The document store, or <c>null</c> if the server's connection details could not be determined.
        /// </returns>
        async Task<IDocumentStore> CreateDocumentStore(string serverId)
        {
            if (String.IsNullOrWhiteSpace(serverId))
                throw new ArgumentException("Argument cannot be null, empty, or entirely composed of whitespace: 'serverId'.", nameof(serverId));
            
            Uri serverBaseAddress = await GetServerBaseAddress(serverId);
            
            var documentStore = new DocumentStore
            {
                Urls = new[] { serverBaseAddress.AbsoluteUri }
            };

            return documentStore.Initialize();
        }

        /// <summary>
        ///     Determine the connection string for the specified server.
        /// </summary>
        /// <param name="serverId">
        ///     The Id of the target server.
        /// </param>
        /// <returns>
        ///     The base UR.
        /// </returns>
        async Task<Uri> GetServerBaseAddress(string serverId)
        {
            if (String.IsNullOrWhiteSpace(serverId))
                throw new ArgumentException("Argument cannot be null, empty, or entirely composed of whitespace: 'serverId'.", nameof(serverId));
            
            Log.LogInformation("Determining connection string for server {ServerId}...",
                serverId
            );

            DatabaseServer targetServer = await GetServer(serverId);

            List<ServiceV1> matchingServices = await KubeClient.ServicesV1().List(
                labelSelector: $"cloud.dimensiondata.daas.server-id = {serverId},cloud.dimensiondata.daas.service-type = internal",
                kubeNamespace: KubeOptions.KubeNamespace
            );
            if (matchingServices.Count == 0)
            {
                Log.LogWarning("Cannot determine connection string for server {ServerId} (server's associated Kubernetes Service not found).",
                    serverId
                );

                throw RespondWith(NotFound(new
                {
                    Reason = "EndPointNotFound",
                    Id = serverId,
                    EntityType = "DatabaseServer",
                    Message = $"Cannot determine base address for server '{targetServer.Id}'."
                }));
            }

            ServiceV1 serverService = matchingServices[matchingServices.Count - 1];
            string serverFQDN = $"{serverService.Metadata.Name}.{serverService.Metadata.Namespace}.svc.cluster.local";
            int serverPort = serverService.Spec.Ports[0].Port;

            Log.LogInformation("Database proxy will connect to RavenDB server '{ServerFQDN}' on {ServerPort}.", serverFQDN, serverPort);

            return new Uri($"http://{serverFQDN}:{serverPort}");
        }

        /// <summary>
        ///     Retrieve and validate the target database server.
        /// </summary>
        /// <param name="serverId">
        ///     The target server Id.
        /// </param>
        /// <returns>
        ///     A <see cref="DatabaseServer"/> representing the database server.
        /// </returns>
        async Task<DatabaseServer> GetServer(string serverId)
        {
            if (String.IsNullOrWhiteSpace(serverId))
                throw new ArgumentException("Argument cannot be null, empty, or entirely composed of whitespace: 'serverId'.", nameof(serverId));
            
            DatabaseServer targetServer = await DocumentSession.LoadAsync<DatabaseServer>(serverId);
            if (targetServer == null)
            {
                Log.LogWarning("Cannot determine connection string for server {ServerId} (server not found).",
                    serverId
                );

                throw RespondWith(NotFound(new
                {
                    Reason = "NotFound",
                    Id = serverId,
                    EntityType = "DatabaseServer",
                    Message = $"Database Server not found with Id '{serverId}'."
                }));
            }

            if (targetServer.Kind != DatabaseServerKind.RavenDB)
            {
                Log.LogWarning("Target server {ServerId} is not a RavenDB server (actual server type is {ServerKind}).",
                    serverId,
                    targetServer.Kind
                );

                throw RespondWith(BadRequest(new
                {
                    Reason = "NotSupported",
                    Id = serverId,
                    EntityType = "DatabaseServer",
                    RequiredServerKind = DatabaseServerKind.RavenDB,
                    ActualServerKind = targetServer.Kind,
                    Message = $"Database Server '{serverId}' is not a RavenDB server."
                }));
            }

            return targetServer;
        }

        public static class Requests
        {
            public static readonly HttpRequest StartUnsecuredSetup = HttpRequest.Factory.Json("setup/unsecured");

            public static readonly HttpRequest CompleteSetup = HttpRequest.Factory.Json("setup/finish");

            public static readonly HttpRequest IsServerAlive = HttpRequest.Factory.Json("setup/alive");
        }
    }
}
