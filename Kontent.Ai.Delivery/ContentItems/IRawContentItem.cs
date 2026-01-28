using System.Text.Json;

namespace Kontent.Ai.Delivery.ContentItems;

/// <summary>
/// Internal interface providing type-erased access to raw JSON data for content item mapping.
/// Used by ContentItemMapper to access RawItemJson on linked items without knowing TModel.
/// </summary>
internal interface IRawContentItem : IContentItem
{
    /// <summary>
    /// The raw JSON of the full content item captured during deserialization.
    /// Contains system, elements, and any other properties from the API response.
    /// </summary>
    JsonElement? RawItemJson { get; }
}
