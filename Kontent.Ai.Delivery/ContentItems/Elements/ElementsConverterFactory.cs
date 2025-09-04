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
            return new DynamicElementsJsonConverter();
        }

        var converterType = typeof(StronglyTypedElementsJsonConverter<>).MakeGenericType(typeToConvert);
        return (JsonConverter)Activator.CreateInstance(converterType)!;
    }

    private sealed class DynamicElementsJsonConverter : JsonConverter<IElementsModel>
    {
        public override IElementsModel Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var document = JsonDocument.ParseValue(ref reader);
            var root = document.RootElement;

            if (root.ValueKind != JsonValueKind.Object)
            {
                return new DynamicElements(new Dictionary<string, JsonElement>(StringComparer.Ordinal));
            }

            var dict = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
            foreach (var prop in root.EnumerateObject())
            {
                dict[prop.Name] = CloneElement(prop.Value);
            }

            return new DynamicElements(dict);
        }

        public override void Write(Utf8JsonWriter writer, IElementsModel value, JsonSerializerOptions options)
        {
            throw new NotSupportedException("Serialization of IElementsModel is not supported.");
        }

        private static JsonElement CloneElement(JsonElement element)
        {
            using var doc = JsonDocument.Parse(element.GetRawText());
            return doc.RootElement.Clone();
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
                var fallback = JsonSerializer.Deserialize<TModel>(elementsRoot.GetRawText(), options);
                return fallback!;
            }

            var jsonObject = new JsonObject();
            foreach (var prop in elementsRoot.EnumerateObject())
            {
                if (prop.Value.ValueKind == JsonValueKind.Object && prop.Value.TryGetProperty("value", out var value))
                {
                    jsonObject[prop.Name] = JsonNode.Parse(value.GetRawText());
                }
                else
                {
                    jsonObject[prop.Name] = null;
                }
            }

            var flattenedJson = jsonObject.ToJsonString();
            var model = JsonSerializer.Deserialize<TModel>(flattenedJson, options);
            return model!;
        }

        public override void Write(Utf8JsonWriter writer, TModel value, JsonSerializerOptions options)
        {
            throw new NotSupportedException("Serialization of elements model is not supported.");
        }
    }
}


