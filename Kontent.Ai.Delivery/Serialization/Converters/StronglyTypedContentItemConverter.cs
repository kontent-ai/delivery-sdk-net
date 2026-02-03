using System.Text.Json;
using System.Text.Json.Serialization;
using Kontent.Ai.Delivery.ContentItems;

namespace Kontent.Ai.Delivery.Serialization.Converters;

/// <summary>
/// Deserializes ContentItem for strongly-typed models.
/// Creates an empty model instance that will be fully populated by ContentItemMapper.
///
/// This converter handles:
/// - Parsing system attributes
/// - Cloning RawItemJson for post-processing
/// - Creating an empty model instance
///
/// The ContentItemMapper is responsible for:
/// - Populating all properties (simple and complex types)
/// - Understanding element envelope structure
/// - Type conversions and hydration
/// </summary>
internal sealed class StronglyTypedContentItemConverter<TModel> : JsonConverter<ContentItem<TModel>>
{
    public override ContentItem<TModel> Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        // 1. Parse system attributes
        if (!root.TryGetProperty("system", out var systemElement))
        {
            throw new JsonException("Missing required 'system' property in content item JSON.");
        }

        var system = JsonSerializer.Deserialize<ContentItemSystemAttributes>(systemElement, options)
            ?? throw new JsonException("Failed to deserialize 'system' property in content item JSON.");

        // 2. Capture full item JSON for post-processing (hydration) and runtime type resolution
        var rawItemJson = root.Clone();

        // 3. Create empty instance - ContentItemMapper will populate ALL properties
        var elements = Activator.CreateInstance<TModel>()
            ?? throw new JsonException($"Failed to create instance of '{typeof(TModel).Name}'.");

        return new ContentItem<TModel>
        {
            System = system,
            Elements = elements,
            RawItemJson = rawItemJson
        };
    }

    public override void Write(Utf8JsonWriter writer, ContentItem<TModel> value, JsonSerializerOptions options)
        => throw new NotSupportedException("Serialization of ContentItem is not supported.");
}
