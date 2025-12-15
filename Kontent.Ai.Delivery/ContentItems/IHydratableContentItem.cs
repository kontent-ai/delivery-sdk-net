using System.Text.Json;

namespace Kontent.Ai.Delivery.ContentItems;

/// <summary>
/// Internal adapter interface used by the hydration pipeline to access strongly-typed content items
/// without reflection.
/// </summary>
internal interface IHydratableContentItem
{
    ContentItemSystemAttributes SystemAttributes { get; }
    object ElementsObject { get; }
    JsonElement? RawElements { get; }
}


