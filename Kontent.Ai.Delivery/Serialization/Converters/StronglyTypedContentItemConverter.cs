using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Kontent.Ai.Delivery.ContentItems;

namespace Kontent.Ai.Delivery.Serialization.Converters;

/// <summary>
/// Deserializes ContentItem for strongly-typed models by flattening element structure.
///
/// Flattening process:
/// - Extracts the "value" property from each element
/// - Sets complex types (rich_text, taxonomy, asset) to null for post-processing
/// - Simple types (text, number, datetime) are deserialized directly
///
/// Example transformation:
/// FROM: { "title": { "type": "text", "name": "Title", "value": "Hello" } }
/// TO:   { "title": "Hello" }
/// </summary>
internal sealed class StronglyTypedContentItemConverter<TModel> : JsonConverter<ContentItem<TModel>>
{
    /// <summary>
    /// Cache for JsonSerializerOptions without the ContentItemConverterFactory.
    /// Uses ConditionalWeakTable to allow GC of options that are no longer referenced elsewhere
    /// while caching those that are still in use. This is critical for performance as
    /// JsonSerializerOptions caches type metadata internally.
    /// </summary>
    private static readonly ConditionalWeakTable<JsonSerializerOptions, JsonSerializerOptions> OptionsCache = [];

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

        var systemJson = systemElement.GetRawText();
        var system = JsonSerializer.Deserialize<ContentItemSystemAttributes>(systemJson, options)
            ?? throw new JsonException("Failed to deserialize 'system' property in content item JSON.");

        // 2. Get elements JsonElement
        var elementsElement = root.TryGetProperty("elements", out var els)
            ? els
            : default;

        // 3. Capture full item JSON for post-processing (hydration) and runtime type resolution
        var rawItemJson = root.CloneElement();

        // 4. Parse elements in strongly-typed mode - flatten structure
        var elements = ParseStronglyTypedElements(elementsElement, options);

        return new ContentItem<TModel>
        {
            System = system,
            Elements = elements,
            RawItemJson = rawItemJson
        };
    }

    public override void Write(Utf8JsonWriter writer, ContentItem<TModel> value, JsonSerializerOptions options)
        => throw new NotSupportedException("Serialization of ContentItem is not supported.");

    /// <summary>
    /// Parses elements in strongly-typed mode by flattening the structure.
    /// Extracts only the "value" property from each element for simple types.
    /// Complex types (rich_text, taxonomy, asset) are set to null and will be
    /// hydrated later by ContentItemMapper.
    /// </summary>
    private static TModel ParseStronglyTypedElements(JsonElement elementsElement, JsonSerializerOptions options)
    {
        if (elementsElement.ValueKind != JsonValueKind.Object)
        {
            // Fallback for non-object elements - deserialize directly
            return JsonSerializer.Deserialize<TModel>(
                elementsElement.GetRawText(),
                GetOptionsWithoutContentItemConverter(options))
                ?? throw new JsonException($"Failed to deserialize content item elements to type '{typeof(TModel).Name}'.");
        }

        // Flatten elements: extract only the "value" JSON into a synthetic object payload.
        // Use Utf8JsonWriter to avoid JsonNode/JsonObject allocations and ToJsonString roundtrip.
        var buffer = new ArrayBufferWriter<byte>(4096);
        using var writer = new Utf8JsonWriter(buffer);

        writer.WriteStartObject();

        foreach (var prop in elementsElement.EnumerateObject())
        {
            writer.WritePropertyName(prop.Name);

            if (prop.Value.ValueKind != JsonValueKind.Object)
            {
                writer.WriteNullValue();
                continue;
            }

            var elementType = prop.Value.GetElementType();

            if (!prop.Value.TryGetProperty("value", out var value))
            {
                writer.WriteNullValue();
                continue;
            }

            // Complex types are set to null - they will be hydrated later.
            if (JsonElementExtensions.IsComplexElementType(elementType))
            {
                writer.WriteNullValue();
                continue;
            }

            // Simple types write their "value" JSON as-is (string/number/bool/array/object).
            value.WriteTo(writer);
        }

        writer.WriteEndObject();
        writer.Flush();

        // Deserialize the flattened JSON without triggering this converter again
        return JsonSerializer.Deserialize<TModel>(
            buffer.WrittenSpan,
            GetOptionsWithoutContentItemConverter(options))
            ?? throw new JsonException($"Failed to deserialize content item elements to type '{typeof(TModel).Name}'.");
    }

    /// <summary>
    /// Gets or creates a copy of JsonSerializerOptions without the ContentItemConverterFactory
    /// to prevent infinite recursion during deserialization. Results are cached to preserve
    /// STJ's internal metadata caching and avoid allocations on every deserialization.
    /// </summary>
    private static JsonSerializerOptions GetOptionsWithoutContentItemConverter(JsonSerializerOptions options)
    {
        return OptionsCache.GetValue(options, static opts =>
        {
            var newOptions = new JsonSerializerOptions(opts);
            var converterToRemove = newOptions.Converters.FirstOrDefault(c => c is ContentItemConverterFactory);
            if (converterToRemove is not null)
            {
                newOptions.Converters.Remove(converterToRemove);
            }
            return newOptions;
        });
    }
}