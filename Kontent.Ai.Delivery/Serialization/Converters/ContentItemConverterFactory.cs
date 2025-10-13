using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Kontent.Ai.Delivery.ContentItems;

/// <summary>
/// Unified JSON converter factory for ContentItem that handles both deserialization and element processing in a single pass.
/// Eliminates double parsing by processing elements inline based on whether the model is dynamic or strongly-typed.
/// </summary>
internal sealed class ContentItemConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
        => typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(ContentItem<>);

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var modelType = typeToConvert.GetGenericArguments()[0];
        var converterType = typeof(ContentItemJsonConverter<>).MakeGenericType(modelType);
        return (JsonConverter)Activator.CreateInstance(converterType)!;
    }

    private sealed class ContentItemJsonConverter<TModel> : JsonConverter<ContentItem<TModel>>
        where TModel : IElementsModel
    {
        public override ContentItem<TModel> Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            // Single JsonDocument parse for the entire ContentItem
            using var doc = JsonDocument.ParseValue(ref reader);
            var root = doc.RootElement;

            // 1. Parse system attributes (simple deserialization)
            var systemJson = root.GetProperty("system").GetRawText();
            var system = JsonSerializer.Deserialize<ContentItemSystemAttributes>(systemJson, options)!;

            // 2. Get elements JsonElement
            var elementsElement = root.TryGetProperty("elements", out var els)
                ? els
                : default;

            // 3. Capture raw elements for enrichers (clone to avoid disposal issues)
            var rawElements = elementsElement.ValueKind != JsonValueKind.Undefined
                && elementsElement.ValueKind != JsonValueKind.Null
                ? CloneElement(elementsElement)
                : (JsonElement?)null;

            // 4. Parse elements based on type (dynamic vs strongly-typed)
            var elements = IsDynamicMode(typeof(TModel))
                ? ParseDynamicElements(elementsElement)
                : ParseStronglyTypedElements(elementsElement, options);

            return new ContentItem<TModel>
            {
                System = system,
                Elements = elements,
                RawElements = rawElements
            };
        }

        /// <summary>
        /// Determines if the model type should be processed in dynamic mode.
        /// </summary>
        private static bool IsDynamicMode(Type modelType)
            => modelType == typeof(IElementsModel) || modelType == typeof(DynamicElements);

        /// <summary>
        /// Parses elements in dynamic mode, preserving full element structure (type, name, value).
        /// </summary>
        private static TModel ParseDynamicElements(JsonElement elementsElement)
        {
            var map = new Dictionary<string, JsonElement>(StringComparer.Ordinal);

            if (elementsElement.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in elementsElement.EnumerateObject())
                {
                    // Store the WHOLE element object (clone to ensure lifetime)
                    map[prop.Name] = CloneElement(prop.Value);
                }
            }

            return (TModel)(IElementsModel)new DynamicElements(map);
        }

        /// <summary>
        /// Parses elements in strongly-typed mode, flattening the structure by extracting only the "value" property.
        /// Complex types (rich_text, taxonomy, asset) are set to null for post-processing by enrichers.
        /// </summary>
        private static TModel ParseStronglyTypedElements(JsonElement elementsElement, JsonSerializerOptions options)
        {
            if (elementsElement.ValueKind != JsonValueKind.Object)
            {
                // Fallback for non-object elements - deserialize directly
                return JsonSerializer.Deserialize<TModel>(
                    elementsElement.GetRawText(),
                    CreateOptionsWithoutContentItemConverter(options))!;
            }

            // Flatten elements: extract "value" property from each element
            var jsonObject = new JsonObject();

            foreach (var prop in elementsElement.EnumerateObject())
            {
                if (prop.Value.ValueKind == JsonValueKind.Object)
                {
                    // Detect element type for special handling
                    var elementType = GetElementType(prop.Value);

                    if (prop.Value.TryGetProperty("value", out var value))
                    {
                        // Set to null for complex types that will be handled by enrichers
                        if (IsComplexElementType(elementType))
                        {
                            jsonObject[prop.Name] = null;
                        }
                        else
                        {
                            jsonObject[prop.Name] = JsonNode.Parse(value.GetRawText());
                        }
                    }
                    else
                    {
                        jsonObject[prop.Name] = null;
                    }
                }
                else
                {
                    jsonObject[prop.Name] = null;
                }
            }

            var flattenedJson = jsonObject.ToJsonString();

            // Deserialize the flattened JSON without triggering this converter again
            return JsonSerializer.Deserialize<TModel>(
                flattenedJson,
                CreateOptionsWithoutContentItemConverter(options))!;
        }

        /// <summary>
        /// Checks if an element type requires complex post-processing by enrichers.
        /// </summary>
        private static bool IsComplexElementType(string? elementType)
            => string.Equals(elementType, "rich_text", StringComparison.OrdinalIgnoreCase)
            || string.Equals(elementType, "taxonomy", StringComparison.OrdinalIgnoreCase)
            || string.Equals(elementType, "asset", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Extracts the "type" property from an element JsonElement.
        /// </summary>
        private static string? GetElementType(JsonElement element)
        {
            return element.TryGetProperty("type", out var typeProp)
                && typeProp.ValueKind == JsonValueKind.String
                ? typeProp.GetString()
                : null;
        }

        /// <summary>
        /// Clones a JsonElement to ensure it survives beyond the JsonDocument lifetime.
        /// </summary>
        private static JsonElement CloneElement(JsonElement element)
        {
            using var doc = JsonDocument.Parse(element.GetRawText());
            return doc.RootElement.Clone();
        }

        /// <summary>
        /// Creates a copy of JsonSerializerOptions without the ContentItemConverterFactory to prevent infinite recursion.
        /// </summary>
        private static JsonSerializerOptions CreateOptionsWithoutContentItemConverter(JsonSerializerOptions options)
        {
            var newOptions = new JsonSerializerOptions(options);
            newOptions.Converters.Remove(
                newOptions.Converters.FirstOrDefault(c => c is ContentItemConverterFactory));
            return newOptions;
        }

        public override void Write(Utf8JsonWriter writer, ContentItem<TModel> value, JsonSerializerOptions options)
            => throw new NotSupportedException("Serialization of ContentItem is not supported.");
    }
}