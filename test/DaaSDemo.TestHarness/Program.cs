using Akka.Actor;
using Akka.Actor.Dsl;
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
    using Akka.Configuration;
    using KubeClient;
    using KubeClient.Clients;
    using KubeClient.Models;
    using Provisioning.Actors;
    using Provisioning.Filters;
    using Provisioning.Messages;

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
            Uri endPointUri = new Uri(
                Environment.GetEnvironmentVariable("KUBE_API_ENDPOINT")
            );
            string accessToken = Environment.GetEnvironmentVariable("KUBE_API_TOKEN");

            using (KubeApiClient kubeClient = KubeApiClient.Create(endPointUri, accessToken))
            {   
                Config config = ConfigurationFactory.ParseString(@"
                    akka {
                        loglevel = INFO,
                        loggers = [""Akka.Logger.Serilog.SerilogLogger, Akka.Logger.Serilog""],
                        suppress-json-serializer-warning = true
                    }
                ");
                using (ActorSystem system = ActorSystem.Create("test-harness", config))
                {
                    IActorRef eventBusActor = system.ActorOf(Props.Create(
                        () => new ReplicationControllerEvents(kubeClient)
                    ));
                    
                    IActorRef listener = system.ActorOf(actor =>
                    {
                        actor.OnPreStart = context =>
                        {
                            Log.Information("Subscribing...");

                            eventBusActor.Tell(SubscribeResourceEvents.Create(
                                filter: ResourceEventFilter.Empty
                            ));

                            Log.Information("Subscribed.");
                        };

                        actor.Receive<ResourceEventV1<V1ReplicationController>>((resourceEvent, context) =>
                        {
                            Log.Information("Recieved {EventType} event for ReplicationController {ResourceName}.",
                                resourceEvent.EventType,
                                resourceEvent.Resource?.Metadata?.Name
                            );
                        });
                    });

                    Log.Information("Running; press enter to terminate.");

                    Console.ReadLine();

                    await system.Terminate();
                }
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
