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
}