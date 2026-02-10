using System.Collections.Concurrent;
using System.Text.Json;

namespace Kontent.Ai.Delivery.ContentItems;

/// <summary>
/// Default implementation of <see cref="IContentDeserializer"/> using System.Text.Json
/// with cached generic type construction for performance.
/// </summary>
/// <remarks>
/// Initializes a new instance of <see cref="ContentDeserializer"/>.
/// </remarks>
/// <param name="options">The JSON serializer options to use.</param>
internal sealed class ContentDeserializer(JsonSerializerOptions options) : IContentDeserializer
{
    private readonly JsonSerializerOptions _options = options ?? throw new ArgumentNullException(nameof(options));

    // Cache constructed ContentItem<T> types to avoid MakeGenericType overhead on repeated calls
    private static readonly ConcurrentDictionary<Type, Type> _contentItemTypes = new();

    /// <summary>
    /// Deserializes JSON to ContentItem&lt;TModel&gt; where TModel is the specified modelType.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="modelType">The model type (any POCO or <see cref="IDynamicElements"/>).</param>
    /// <returns>The deserialized ContentItem as an object.</returns>
    public object DeserializeContentItem(string json, Type modelType)
    {
        if (string.IsNullOrEmpty(json))
        {
            throw new ArgumentException("JSON cannot be null or empty", nameof(json));
        }

        ArgumentNullException.ThrowIfNull(modelType);

        var contentItemType = _contentItemTypes.GetOrAdd(modelType,
            static t => typeof(ContentItem<>).MakeGenericType(t));

        return JsonSerializer.Deserialize(json, contentItemType, _options)
            ?? throw new JsonException($"Deserialization returned null for {contentItemType.Name}");
    }

    /// <summary>
    /// Deserializes a JsonElement to ContentItem&lt;TModel&gt; where TModel is the specified modelType.
    /// Avoids the string allocation of GetRawText() when the source is already a JsonElement.
    /// </summary>
    /// <param name="jsonElement">The JsonElement to deserialize.</param>
    /// <param name="modelType">The model type (any POCO or <see cref="IDynamicElements"/>).</param>
    /// <returns>The deserialized ContentItem as an object.</returns>
    public object DeserializeContentItem(JsonElement jsonElement, Type modelType)
    {
        ArgumentNullException.ThrowIfNull(modelType);

        var contentItemType = _contentItemTypes.GetOrAdd(modelType,
            static t => typeof(ContentItem<>).MakeGenericType(t));

        return JsonSerializer.Deserialize(jsonElement, contentItemType, _options)
            ?? throw new JsonException($"Deserialization returned null for {contentItemType.Name}");
    }
}
