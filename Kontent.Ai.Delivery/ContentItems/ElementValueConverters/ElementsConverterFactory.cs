using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Kontent.Ai.Delivery.ContentItems;

/// <summary>
/// Single factory that produces converters for the Delivery elements node:
/// - DynamicElementsJsonConverter for IElementsModel (dynamic mode, keeps the original JsonElement structure with metadata)
/// - StronglyTypedElementsJsonConverter for TModel : IElementsModel (flatten elements node, use only value property for simplicity)
/// </summary>
internal sealed class ElementsConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        if (typeToConvert == typeof(IElementsModel))
        {
            return true;
        }

        if (typeof(IElementsModel).IsAssignableFrom(typeToConvert))
        {
            // Exclude DynamicElements (handled by IElementsModel case) and abstract/interface types
            return typeToConvert != typeof(DynamicElements) && !typeToConvert.IsInterface && !typeToConvert.IsAbstract;
        }

        return false;
    }

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        if (typeToConvert == typeof(IElementsModel))
        {
            return new DynamicElementsConverter();
        }

        var converterType = typeof(StronglyTypedElementsJsonConverter<>).MakeGenericType(typeToConvert);
        return (JsonConverter)Activator.CreateInstance(converterType)!;
    }

    internal sealed class DynamicElementsConverter : JsonConverter<IElementsModel>
    {
        public override IElementsModel Read(ref Utf8JsonReader reader, Type _, JsonSerializerOptions __)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            var root = doc.RootElement;

            var map = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
            if (root.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in root.EnumerateObject())
                {
                    // store the WHOLE element object (clone to ensure lifetime)
                    map[prop.Name] = Clone(prop.Value);
                }
            }
            return new DynamicElements(map);
        }

        public override void Write(Utf8JsonWriter writer, IElementsModel value, JsonSerializerOptions options)
            => throw new NotSupportedException();

        private static JsonElement Clone(JsonElement el)
        {
            using var d = JsonDocument.Parse(el.GetRawText());
            return d.RootElement.Clone();
        }
    }

    private sealed class StronglyTypedElementsJsonConverter<TModel> : JsonConverter<TModel> where TModel : IElementsModel
    {
        public override TModel Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var document = JsonDocument.ParseValue(ref reader);
            var elementsRoot = document.RootElement;

            if (elementsRoot.ValueKind != JsonValueKind.Object)
            {
                // Create new options without the ElementsConverterFactory to avoid infinite recursion
                var fallbackOptions = new JsonSerializerOptions(options);
                fallbackOptions.Converters.Remove(fallbackOptions.Converters.FirstOrDefault(c => c is ElementsConverterFactory));

                var fallback = JsonSerializer.Deserialize<TModel>(elementsRoot.GetRawText(), fallbackOptions);
                return fallback!;
            }

            var jsonObject = new JsonObject();
            foreach (var prop in elementsRoot.EnumerateObject())
            {
                if (prop.Value.ValueKind == JsonValueKind.Object)
                {
                    // Prefer safe defaults for complex element types that are handled later by post-processing
                    // Detect element type when available
                    string? elementType = null;
                    if (prop.Value.TryGetProperty("type", out var typeProp) && typeProp.ValueKind == JsonValueKind.String)
                    {
                        elementType = typeProp.GetString();
                    }

                    if (prop.Value.TryGetProperty("value", out var value))
                    {
                        // For rich_text, taxonomy, and asset elements, set null to avoid interface deserialization issues
                        if (string.Equals(elementType, "rich_text", StringComparison.OrdinalIgnoreCase)
                            || string.Equals(elementType, "taxonomy", StringComparison.OrdinalIgnoreCase)
                            || string.Equals(elementType, "asset", StringComparison.OrdinalIgnoreCase))
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

            // Create new options without the ElementsConverterFactory to avoid infinite recursion
            var newOptions = new JsonSerializerOptions(options);
            newOptions.Converters.Remove(newOptions.Converters.FirstOrDefault(c => c is ElementsConverterFactory));

            var model = JsonSerializer.Deserialize<TModel>(flattenedJson, newOptions);
            return model!;
        }

        public override void Write(Utf8JsonWriter writer, TModel value, JsonSerializerOptions options)
        {
            throw new NotSupportedException("Serialization of elements model is not supported.");
        }
    }
}


