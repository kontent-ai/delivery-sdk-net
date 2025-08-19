using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Kontent.Ai.Delivery.Abstractions.Serialization;
using Kontent.Ai.Delivery.Serialization.Converters;

namespace Kontent.Ai.Delivery.Serialization
{
    /// <summary>
    /// Delivery SDK JSON serializer using System.Text.Json.
    /// </summary>
    public class DeliveryJsonSerializer : IJsonSerializer
    {
        private readonly JsonSerializerOptions _options;

        public DeliveryJsonSerializer(IServiceProvider serviceProvider)
        {
            _options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                WriteIndented = false,
                PropertyNameCaseInsensitive = true,
                NumberHandling = JsonNumberHandling.AllowReadingFromString,
                Converters =
                {
                    new JsonStringEnumConverter(JsonNamingPolicy.CamelCase),
                    new DateTimeOffsetConverter()
                }
            };

            // Add custom type resolver for DI support
            _options.TypeInfoResolver = new DeliveryTypeInfoResolver(serviceProvider);
        }

        public string Serialize(object value)
        {
            return JsonSerializer.Serialize(value, value?.GetType() ?? typeof(object), _options);
        }

        public string Serialize<T>(T value)
        {
            return JsonSerializer.Serialize(value, _options);
        }

        public T Deserialize<T>(string json)
        {
            return JsonSerializer.Deserialize<T>(json, _options);
        }

        public object Deserialize(string json, Type type)
        {
            return JsonSerializer.Deserialize(json, type, _options);
        }

        public async Task<T> DeserializeAsync<T>(Stream stream, CancellationToken cancellationToken = default)
        {
            return await JsonSerializer.DeserializeAsync<T>(stream, _options, cancellationToken).ConfigureAwait(false);
        }

        public async Task SerializeAsync<T>(Stream stream, T value, CancellationToken cancellationToken = default)
        {
            await JsonSerializer.SerializeAsync(stream, value, _options, cancellationToken).ConfigureAwait(false);
        }
    }
}