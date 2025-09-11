using System.Text.Json;
using System.Text.Json.Serialization;

namespace Kontent.Ai.Delivery.ContentItems
{
    /// <summary>
    /// Json converter factory for ContentItem that captures the raw elements object
    /// to enable post-processing (e.g., hydrating IRichTextContent).
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

        private sealed class ContentItemJsonConverter<TModel> : JsonConverter<ContentItem<TModel>> where TModel : IElementsModel
        {
            public override ContentItem<TModel> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                using var doc = JsonDocument.ParseValue(ref reader);
                var root = doc.RootElement;

                var system = root.GetProperty("system").GetRawText();
                var elements = root.TryGetProperty("elements", out var els) ? els : default;

                var systemObj = JsonSerializer.Deserialize<ContentItemSystemAttributes>(system, options)!;
                var elementsObj = JsonSerializer.Deserialize<TModel>(elements.GetRawText(), options)!;

                return new ContentItem<TModel>
                {
                    System = systemObj,
                    Elements = elementsObj,
                    RawElements = elements
                };
            }

            public override void Write(Utf8JsonWriter writer, ContentItem<TModel> value, JsonSerializerOptions options)
                => throw new NotSupportedException();
        }
    }
}


