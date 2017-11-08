using HTTPlease;
using KubeNET.Swagger.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace DaaSDemo.TestHarness
{
    using KubeClient;
    using KubeClient.Clients;
    using KubeClient.Models;

    /// <summary>
    ///     A general-purpose test harness.
    /// </summary>
    static class Program
    {
        /// <summary>
        ///     The asynchronous program entry-point.
        /// </summary>
        /// <returns>
        ///     A <see cref="Task"/> representing program execution.
        /// </returns>
        static async Task AsyncMain()
        {
            await Task.Yield(); // Remove this if our test is genuinely async.

            Uri endPointUri = new Uri(
                Environment.GetEnvironmentVariable("KUBE_API_ENDPOINT")
            );
            string accessToken = Environment.GetEnvironmentVariable("KUBE_API_TOKEN");

            using (KubeApiClient kubeClient = KubeApiClient.Create(endPointUri, accessToken))
            {   
                Log.Information("Preparing to stream from {ApiEndPoint}...",
                    kubeClient.Http.BaseAddress
                );

                // AF: Note - watching ReplicationControllers does not work via Rancher API proxy (but does work via kubectl proxy).
                IObservable<V1ResourceEvent<V1ReplicationController>> events = kubeClient.ReplicationControllersV1.WatchAll();
                IDisposable subscription = events.Subscribe(
                    onNext: eventData =>
                    {
                        Log.Information("Got {EventKind} event:\n{@Resource}", eventData.EventType, eventData.Resource);
                    },
                    onCompleted: () =>
                    {
                        Log.Information("End of stream.");
                    },
                    onError: error =>
                    {
                        Log.Error(error, "Error from event stream: {ErrorMessage}", error);
                    }
                );

                Thread.Sleep(
                    TimeSpan.FromSeconds(30)
                );

                subscription.Dispose();
            }
        }

        /// <summary>
        ///     The main program entry-point.
        /// </summary>
        static void Main()
        {
            SynchronizationContext.SetSynchronizationContext(
                new SynchronizationContext()
            );
            ConfigureLogging();

            try
            {
                AsyncMain().GetAwaiter().GetResult();
            }
            catch (AggregateException unexpectedErrorFromTask)
            {
                foreach (Exception unexpectedError in unexpectedErrorFromTask.InnerExceptions)
                    Log.Error(unexpectedError, "Unexpected error: {ErrorMessage}", unexpectedError.Message);
            }
            catch (Exception unexpectedError)
            {
                Log.Error(unexpectedError, "Unexpected error: {ErrorMessage}", unexpectedError.Message);
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        /// <summary>
        ///     Configure the global logger.
        /// </summary>
        static void ConfigureLogging()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .Enrich.FromLogContext()
                .WriteTo.Debug()
                .WriteTo.LiterateConsole()
                .CreateLogger();
        }
    }
}
