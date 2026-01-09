using System.Text.Json;

namespace Kontent.Ai.Delivery.ContentItems;

/// <summary>
/// Internal interface providing type-erased access to raw JSON data for content item mapping.
/// Used by ContentItemMapper to access RawElements on linked items without knowing TModel.
/// </summary>
internal interface IRawContentItem
{
    /// <summary>
    /// The strongly-typed elements object.
    /// </summary>
    object Elements { get; }

    /// <summary>
    /// The raw JSON element envelope captured during deserialization.
    /// </summary>
    JsonElement? RawElements { get; }
}
