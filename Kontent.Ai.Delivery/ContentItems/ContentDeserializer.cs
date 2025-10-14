using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Text.Json;

namespace Kontent.Ai.Delivery.ContentItems;

/// <summary>
/// Default implementation of <see cref="IContentDeserializer"/> using System.Text.Json
/// with cached compiled delegates for performance.
/// </summary>
/// <remarks>
/// Initializes a new instance of <see cref="ContentDeserializer"/>.
/// </remarks>
/// <param name="options">The JSON serializer options to use.</param>
internal sealed class ContentDeserializer(JsonSerializerOptions options) : IContentDeserializer
{
    private readonly JsonSerializerOptions _options = options ?? throw new ArgumentNullException(nameof(options));

    // Cache compiled delegates per model type to avoid reflection overhead on repeated calls
    private static readonly ConcurrentDictionary<Type, Func<string, JsonSerializerOptions, object>> _compiledDeserializers = new();

    /// <summary>
    /// Deserializes JSON to ContentItem&lt;TModel&gt; where TModel is the specified modelType.
    /// Uses cached compiled delegates for optimal performance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="modelType">The model type implementing <see cref="IElementsModel"/>.</param>
    /// <returns>The deserialized ContentItem as an object.</returns>
    public object DeserializeContentItem(string json, Type modelType)
    {
        if (string.IsNullOrEmpty(json))
        {
            throw new ArgumentException("JSON cannot be null or empty", nameof(json));
        }

        if (modelType == null)
        {
            throw new ArgumentNullException(nameof(modelType));
        }

        // Validate that modelType implements IElementsModel
        if (!typeof(IElementsModel).IsAssignableFrom(modelType))
        {
            throw new ArgumentException(
                $"Model type {modelType.Name} must implement {nameof(IElementsModel)}",
                nameof(modelType));
        }

        var deserializer = _compiledDeserializers.GetOrAdd(modelType, BuildDeserializer);
        return deserializer(json, _options);
    }

    /// <summary>
    /// Builds a compiled delegate for deserializing to ContentItem&lt;modelType&gt;.
    /// The delegate signature is: (string json, JsonSerializerOptions options) => object
    /// </summary>
    private static Func<string, JsonSerializerOptions, object> BuildDeserializer(Type modelType)
    {
        // We want to deserialize to ContentItem<modelType>
        var contentItemType = typeof(ContentItem<>).MakeGenericType(modelType);

        // Parameters for the lambda
        var jsonParam = Expression.Parameter(typeof(string), "json");
        var optionsParam = Expression.Parameter(typeof(JsonSerializerOptions), "options");

        // Call: JsonSerializer.Deserialize(json, contentItemType, options)
        Type[] parameterTypes = [typeof(string), typeof(Type), typeof(JsonSerializerOptions)];
        var deserializeMethod = typeof(JsonSerializer)
            .GetMethod(nameof(JsonSerializer.Deserialize), parameterTypes)
            ?? throw new InvalidOperationException("Could not find JsonSerializer.Deserialize method");

        var deserializeCall = Expression.Call(
            deserializeMethod,
            jsonParam,
            Expression.Constant(contentItemType, typeof(Type)),
            optionsParam);

        // Convert result to object
        var convertedResult = Expression.Convert(deserializeCall, typeof(object));

        // Compile the lambda: (json, options) => (object)JsonSerializer.Deserialize(json, contentItemType, options)
        return Expression.Lambda<Func<string, JsonSerializerOptions, object>>(
            convertedResult,
            jsonParam,
            optionsParam).Compile();
    }
}
