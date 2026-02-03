using System.Text.Json;

namespace Kontent.Ai.Delivery.Abstractions;

/// <summary>
/// Provides deserialization of JSON content items into strongly-typed models.
/// </summary>
public interface IContentDeserializer
{
    /// <summary>
    /// Deserializes a JSON string into a ContentItem with the specified model type.
    /// </summary>
    /// <param name="json">The JSON string representing the content item.</param>
    /// <param name="modelType">The model type (any POCO or <see cref="IDynamicElements"/>).</param>
    /// <returns>The deserialized content item as an object (cast to IContentItem&lt;TModel&gt; as needed).</returns>
    object DeserializeContentItem(string json, Type modelType);

    /// <summary>
    /// Deserializes a JsonElement into a ContentItem with the specified model type.
    /// This overload avoids the string allocation of GetRawText() when the source is already a JsonElement.
    /// </summary>
    /// <param name="jsonElement">The JsonElement representing the content item.</param>
    /// <param name="modelType">The model type (any POCO or <see cref="IDynamicElements"/>).</param>
    /// <returns>The deserialized content item as an object (cast to IContentItem&lt;TModel&gt; as needed).</returns>
    /// <remarks>
    /// Default implementation falls back to GetRawText() for backward compatibility with existing implementations.
    /// Override in concrete implementations to avoid the string allocation.
    /// </remarks>
    object DeserializeContentItem(JsonElement jsonElement, Type modelType)
        => DeserializeContentItem(jsonElement.GetRawText(), modelType);
}
