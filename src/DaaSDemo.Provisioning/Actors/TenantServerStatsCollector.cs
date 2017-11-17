using Akka;
using Akka.Actor;
using HTTPlease;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace DaaSDemo.Provisioning.Actors
{
    /// <summary>
    ///     Actor that collects statistics from Prometheus for a tenant's SQL Server instance.
    /// </summary>
    public class TenantServerStatsCollector
        : ReceiveActorEx
    {
        /// <summary>
        ///     The default name for <see cref="TenantServerStatsCollector"/> actors.
        /// </summary>
        public static readonly string ActorName = "stats-collector";

        /// <summary>
        ///     The period between successive collections.
        /// </summary>
        public static readonly TimeSpan PollPeriod = TimeSpan.FromSeconds(10);

        /// <summary>
        ///     HTTP request definition for querying Prometheus.
        /// </summary>
        static readonly HttpRequest PrometheusQuery = HttpRequest.Factory.Json("api/v1/query?query={Query}");

        /// <summary>
        ///     The Id of the target server.
        /// </summary>
        readonly int _serverId;

        /// <summary>
        ///     A reference to the <see cref="DataAccess"/> actor.
        /// </summary>
        readonly IActorRef _dataAccess;

        /// <summary>
        ///     The <see cref="HttpClient"/> used to communicate with Prometheus.
        /// </summary>
        HttpClient _httpClient;

        /// <summary>
        ///     An <see cref="ICancelable"/> used to cancel scheduled poll notifications.
        /// </summary>
        ICancelable _pollCancellation;

        /// <summary>
        ///     Create a new <see cref="TenantServerStatsCollector"/> actor.
        /// </summary>
        /// <param name="serverId">
        ///     The Id of the target server.
        /// </param>
        /// <param name="dataAccess">
        ///     A reference to the <see cref="DataAccess"/> actor.
        /// </param>
        public TenantServerStatsCollector(int serverId, IActorRef dataAccess)
        {
            if (dataAccess == null)
                throw new ArgumentNullException(nameof(dataAccess));

            _serverId = serverId;
            _dataAccess = dataAccess;
            _httpClient = CreateHttpClient();
        }

        /// <summary>
        ///     Called when the actor is ready to handle requests.
        /// </summary>
        void Ready()
        {
            Receive<Signal>(signal =>
            {
                switch (signal)
                {
                    case Signal.Collect:
                    {
                        // TODO: Call the Prometheus API.

                        break;
                    }
                    default:
                    {
                        Unhandled(signal);

                        break;
                    }
                }
            });
        }

        /// <summary>
        ///     Called when the actor is started.
        /// </summary>
        protected override void PreStart()
        {
            _pollCancellation = Context.System.Scheduler.ScheduleTellRepeatedlyCancelable(
                initialDelay: TimeSpan.Zero,
                interval: PollPeriod,
                receiver: Self,
                message: Signal.Collect,
                sender: Self
            );

            Become(Ready);
        }

        /// <summary>
        ///     Called when the actor is stopped.
        /// </summary>
        protected override void PostStop()
        {
            if (_pollCancellation != null)
            {
                _pollCancellation.Cancel();
                _pollCancellation = null;
            }

            if (_httpClient != null)
            {
                _httpClient.Dispose();
                _httpClient = null;
            }
        }

        /// <summary>
        ///     Create a new HTTP client for communicating with the Prometheus API.
        /// </summary>
        /// <returns>
        ///     The configured <see cref="HttpClient"/>.
        /// </returns>
        HttpClient CreateHttpClient()
        {
            return new HttpClient
            {
                BaseAddress = new Uri(
                    Context.System.Settings.Config.GetString("daas.prometheus.api-endpoint")
                )
            };
        }

        /// <summary>
        ///     Generate <see cref="Props"/> for creating a <see cref=""TenantServerStatsCollector/> actor.
        /// </summary>
        /// <param name="serverId">
        ///     The Id of the target server.
        /// </param>
        /// <param name="dataAccess">
        ///     A reference to the <see cref="DataAccess"/> actor.
        /// </param>
        /// <returns>
        ///     The configured <see cref="Props"/>.
        /// </returns>
        public static Props Create(int serverId, IActorRef dataAccess)
        {
            if (dataAccess == null)
                throw new ArgumentNullException(nameof(dataAccess));

            return Props.Create(() => new TenantServerStatsCollector(serverId, dataAccess));
        }

        /// <summary>
        ///     Well-known signals for the <see cref="TenantServerStatsCollector"/> actor.
        /// </summary>
        public enum Signal
        {
            /// <summary>
            ///     Collect statistics for the target server.
            /// </summary>
            Collect = 1
        }

        class PrometheusResponse
        {
            [JsonProperty("status")]
            public string Status { get; set; }

            [JsonProperty("data")]
            public PrometheusQueryResponseData Data { get; set; }
        }

        class PrometheusQueryResponseData
        {
            [JsonProperty("resultType")]
            public string ResultType { get; set; }

            [JsonProperty("result", ObjectCreationHandling = ObjectCreationHandling.Reuse)]
            public List<PrometheusQueryResult> Results { get; } = new List<PrometheusQueryResult>();
        }

        class PrometheusQueryResult
        {
            [JsonProperty("metric", ObjectCreationHandling = ObjectCreationHandling.Reuse)]
            public Dictionary<string, string> Labels { get; } = new Dictionary<string, string>();

            [JsonProperty("value")]
            public PrometheusValue Value { get; set; }
        }

        [JsonConverter(typeof(PrometheusValueConverter))]
        class PrometheusValue
        {
            public DateTime Timestamp { get; set; }
            public JValue Value { get; set; }
        }

        class PrometheusValueConverter
            : JsonConverter
        {
            public override bool CanConvert(Type objectType) => objectType == typeof(PrometheusValue);

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                if (reader.TokenType == JsonToken.Null)
                    return null;

                if (reader.TokenType != JsonToken.StartArray)
                    throw new JsonException("Expected start of array.");

                JArray array = JArray.Load(reader);
                if (array.Count != 2)
                    throw new JsonException("Expected array of length 2.");

                long ticks = array[0].Value<long>();

                JValue value= (JValue)array[1];

                return new PrometheusValue
                {
                    Timestamp = UnixDateTime.FromUnix(
                        array[0].Value<long>()
                    ),
                    Value = (JValue)array[1]
                };
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                if (value is PrometheusValue prometheusValue)
                {
                    writer.WriteStartArray();
                    writer.WriteValue(
                        UnixDateTime.ToUnix(prometheusValue.Timestamp)
                    );
                    writer.WriteValue(prometheusValue.Value);
                    writer.WriteEndArray();
                }
                else if (value == null)
                    writer.WriteNull();
                else
                    throw new JsonException("Expected PrometheusValue.");
            }
        }

        static class UnixDateTime
        {
            static readonly DateTime Epoch = new DateTime(1970, 1, 1);

            public static DateTime FromUnix(long unixDateTime) => Epoch.AddTicks(unixDateTime);

            public static long ToUnix(DateTime dateTime)
            {
                if (dateTime < Epoch)
                    throw new ArgumentOutOfRangeException(nameof(dateTime), dateTime, "Cannot convert a date / time before the Unix epoch.");

                return (dateTime - Epoch).Ticks;
            }
        }
    }
}
