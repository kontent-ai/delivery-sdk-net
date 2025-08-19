using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Kontent.Ai.Delivery.Serialization.Converters
{
    /// <summary>
    /// Custom DateTimeOffset converter that handles various date formats.
    /// </summary>
    internal class DateTimeOffsetConverter : JsonConverter<DateTimeOffset>
    {
        private static readonly string[] DateFormats = new[]
        {
            "yyyy-MM-ddTHH:mm:ssZ",
            "yyyy-MM-ddTHH:mm:ss.fffffffZ",
            "yyyy-MM-ddTHH:mm:sszzz",
            "yyyy-MM-ddTHH:mm:ss.fffffffzzz",
            "yyyy-MM-dd HH:mm:ss",
            "yyyy-MM-dd"
        };

        public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var dateString = reader.GetString();
            
            if (string.IsNullOrEmpty(dateString))
            {
                return DateTimeOffset.MinValue;
            }

            if (DateTimeOffset.TryParse(dateString, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
            {
                return result;
            }

            foreach (var format in DateFormats)
            {
                if (DateTimeOffset.TryParseExact(dateString, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
                {
                    return result;
                }
            }

            throw new JsonException($"Unable to parse DateTimeOffset from '{dateString}'");
        }

        public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString("yyyy-MM-ddTHH:mm:ss.fffffffzzz", CultureInfo.InvariantCulture));
        }
    }

    /// <summary>
    /// Custom DateTime converter that handles various date formats.
    /// </summary>
    internal class DateTimeConverter : JsonConverter<DateTime>
    {
        private static readonly string[] DateFormats = new[]
        {
            "yyyy-MM-ddTHH:mm:ssZ",
            "yyyy-MM-ddTHH:mm:ss.fffffffZ",
            "yyyy-MM-ddTHH:mm:ss",
            "yyyy-MM-dd HH:mm:ss",
            "yyyy-MM-dd"
        };

        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var dateString = reader.GetString();
            
            if (string.IsNullOrEmpty(dateString))
            {
                return DateTime.MinValue;
            }

            if (DateTime.TryParse(dateString, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
            {
                return result;
            }

            foreach (var format in DateFormats)
            {
                if (DateTime.TryParseExact(dateString, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
                {
                    return result;
                }
            }

            throw new JsonException($"Unable to parse DateTime from '{dateString}'");
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ", CultureInfo.InvariantCulture));
        }
    }
}