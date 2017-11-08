using HTTPlease;
using KubeNET.Swagger.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Reactive.Linq;
using System.IO;
using System.Reactive.Subjects;
using Serilog;

namespace DaaSDemo.KubeClient.Clients
{
    using Models;

    /// <summary>
    ///     The base class for Kubernetes resource API clients.
    /// </summary>
    public abstract class KubeResourceClient
    {
        /// <summary>
        ///     JSON serialisation settings.
        /// </summary>
        protected internal static JsonSerializerSettings SerializerSettings => new JsonSerializerSettings
        {
            Converters =
            {
                new StringEnumConverter()
            }
        };

        // TODO: Declare base request definitions (that include the serialiser settings).

        /// <summary>
        ///     Create a new <see cref="KubeResourceClient"/>.
        /// </summary>
        /// <param name="client">
        ///     The Kubernetes API client.
        /// </param>
        public KubeResourceClient(KubeApiClient client)
        {
            if (client == null)
                throw new ArgumentNullException(nameof(client));
            
            Client = client;
        }

        /// <summary>
        ///     The Kubernetes API client.
        /// </summary>
        protected KubeApiClient Client { get; }

        /// <summary>
        ///     The underlying HTTP client.
        /// </summary>
        protected HttpClient Http => Client.Http;

        /// <summary>
        ///     Get a single resource, returning <c>null</c> if it does not exist.
        /// </summary>
        /// <typeparam name="TResource">
        ///     The type of resource to retrieve.
        /// </typeparam>
        /// <param name="request">
        ///     An <see cref="HttpRequest"/> representing the resource to retrieve.
        /// </param>
        /// <param name="cancellationToken">
        ///     An optional <see cref="CancellationToken"/> that can be used to cancel the request.
        /// </param>
        /// <returns>
        ///     A <typeparamref name="TResource"/> representing the current state for the resource, or <c>null</c> if no resource was found with the specified name and namespace.
        /// </returns>
        protected async Task<TResource> GetSingleResource<TResource>(HttpRequest request, CancellationToken cancellationToken = default(CancellationToken))
            where TResource : class
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            
            using (HttpResponseMessage responseMessage = await Http.GetAsync(request, cancellationToken))
            {
                if (responseMessage.IsSuccessStatusCode)
                    return await responseMessage.ReadContentAsAsync<TResource>();

                // Ensure that HttpStatusCode.NotFound actually refers to the ReplicationController.
                UnversionedStatus status = await responseMessage.ReadContentAsAsync<UnversionedStatus, UnversionedStatus>(HttpStatusCode.NotFound);
                if (status.Reason != "NotFound")
                    throw new HttpRequestException<UnversionedStatus>(responseMessage.StatusCode, status);

                return null;
            }
        }

        /// <summary>
        ///     Get an <see cref="IObservable{T}"/> for <see cref="V1ResourceEvent{TResource}"/>s streamed from an HTTP GET request.
        /// </summary>
        /// <typeparam name="TResource">
        ///     The resource type that the events relate to.
        /// </typeparam>
        /// <param name="request">
        ///     The <see cref="HttpRequest"/> to execute.
        /// </param>
        /// <returns>
        ///     The <see cref="IObservable{T}"/>.
        /// </returns>
        protected IObservable<V1ResourceEvent<TResource>> ObserveEvents<TResource>(HttpRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            return ObserveLines(request).Select(
                line => JsonConvert.DeserializeObject<V1ResourceEvent<TResource>>(line, SerializerSettings)
            );
        }

        /// <summary>
        ///     Get an <see cref="IObservable{T}"/> for lines streamed from an HTTP GET request.
        /// </summary>
        /// <param name="request">
        ///     The <see cref="HttpRequest"/> to execute.
        /// </param>
        /// <returns>
        ///     The <see cref="IObservable{T}"/>.
        /// </returns>
        protected IObservable<string> ObserveLines(HttpRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            
            return Observable.Create<string>(async (subscriber, subscriptionCancellation) =>
            {
                try
                {
                    Log.Information("Requesting...");

                    using (HttpResponseMessage responseMessage = await Http.GetStreamedAsync(request, subscriptionCancellation))
                    {
                        responseMessage.EnsureSuccessStatusCode();

                        Log.Information("Streaming from {RequestUri}...", responseMessage.RequestMessage.RequestUri);

                        using (Stream responseStream = await responseMessage.Content.ReadAsStreamAsync())
                        using (StreamReader responseReader = new StreamReader(responseStream))
                        {
                            string line = await responseReader.ReadLineAsync();
                            while (line != null)
                            {
                                Log.Information("Got line.");

                                subscriber.OnNext(line);

                                line = await responseReader.ReadLineAsync();
                            }
                        }
                    }
                }
                catch (OperationCanceledException operationCanceled)
                {
                    if (operationCanceled.CancellationToken == subscriptionCancellation)
                        return; // Not an error.

                    subscriber.OnError(operationCanceled);
                }
                catch (Exception exception)
                {
                    subscriber.OnError(exception);
                }
                finally
                {
                    subscriber.OnCompleted();
                }
            });
        }
    }
}
