using System.Text.Json;

namespace Kontent.Ai.Delivery.ContentItems;

/// <summary>
/// Internal interface providing access to raw JSON data for content item hydration.
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


