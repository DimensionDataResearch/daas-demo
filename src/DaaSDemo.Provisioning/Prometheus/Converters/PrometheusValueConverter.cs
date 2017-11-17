using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace DaaSDemo.Provisioning.Prometheus.Converters
{
    using Common.Utilities;

    /// <summary>
    ///     JSON converter for values from Prometheus queries.
    /// </summary>

    public class PrometheusValueConverter
        : JsonConverter
    {
        /// <summary>
        ///     Determine whether the converter can handle an object of the specified type.
        /// </summary>
        /// <param name="objectType">
        ///     The target object type.
        /// </param>
        /// <returns>
        ///     <c>true</c>, if the converter can handle the object; otherwise, <c>false</c>.
        /// </returns>
        public override bool CanConvert(Type objectType) => objectType == typeof(PrometheusValue);

        /// <summary>
        ///     Read an object from JSON.
        /// </summary>
        /// <param name="reader">
        ///     The <see cref="JsonReader"/> to read from.
        /// </param>
        /// <param name="objectType">
        ///     The type of object to read.
        /// </param>
        /// <param name="existingValue">
        ///     The existing value (if any).
        /// </param>
        /// <param name="serializer">
        ///     The current <see cref="JsonSerializer"/>.
        /// </param>
        /// <returns>
        ///     The deserialised object.
        /// </returns>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            if (reader.TokenType == JsonToken.Null)
                return null;

            if (reader.TokenType != JsonToken.StartArray)
                throw new JsonException("Expected start of array.");

            JArray array = JArray.Load(reader);
            if (array.Count != 2)
                throw new JsonException("Expected array of length 2.");

            long ticks = array[0].Value<long>();

            DateTime timestamp = UnixDateTime.FromUnix(
                array[0].Value<long>()
            );
            JValue jValue = (JValue)array[1];

            return new PrometheusValue
            {
                Timestamp = timestamp,
                Value = jValue.Value.ToString()
            };
        }

        /// <summary>
        ///     Write an object to JSON.
        /// </summary>
        /// <param name="writer">
        ///     The <see cref="JsonWriter"/> to write to.
        /// </param>
        /// <param name="value">
        ///     The value to write.
        /// </param>
        /// <param name="serializer">
        ///     The current <see cref="JsonSerializer"/>.
        /// </param>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (writer == null)
                throw new ArgumentNullException(nameof(writer));

            if (value is PrometheusValue prometheusValue)
            {
                writer.WriteStartArray();

                writer.WriteValue(
                    UnixDateTime.ToUnix(prometheusValue.Timestamp)
                );

                if (prometheusValue.Value != null)
                    writer.WriteValue(prometheusValue.Value);
                else
                    writer.WriteNull();

                writer.WriteEndArray();
            }
            else if (value == null)
                writer.WriteNull();
            else
                throw new JsonException("Expected PrometheusValue.");
        }
    }
}
