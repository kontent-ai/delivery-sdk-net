using System.Text.Json;
using System.Text.Json.Serialization;
using Kontent.Ai.Delivery.ContentItems;

namespace Kontent.Ai.Delivery.Serialization.Converters;

/// <summary>
/// Factory for creating ContentItem converters based on model type.
/// Dispatches to either DynamicContentItemConverter or StronglyTypedContentItemConverter.
/// </summary>
internal sealed class ContentItemConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
        => typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(ContentItem<>);

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var modelType = typeToConvert.GetGenericArguments()[0];

        // Dispatch to appropriate converter based on model type
        return IsDynamicMode(modelType)
            ? (JsonConverter)Activator.CreateInstance(typeof(DynamicContentItemConverter<>).MakeGenericType(modelType))!
            : (JsonConverter)Activator.CreateInstance(typeof(StronglyTypedContentItemConverter<>).MakeGenericType(modelType))!;
    }

    /// <summary>
    /// Determines if the model type should be processed in dynamic mode.
    /// Dynamic mode preserves full element structure for runtime inspection.
    /// </summary>
    private static bool IsDynamicMode(Type modelType)
        => modelType == typeof(IDynamicElements)
        || modelType == typeof(DynamicElements);
}