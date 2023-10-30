using System.Collections.Generic;

namespace Kontent.Ai.Delivery.Abstractions;

/// <summary>
/// Represents a delta update.
/// </summary>
public interface ISyncItemData : IContentItem
{
    /// <summary>
    /// Retrieves key:value pairs representing content item elements.
    /// </summary>
    Dictionary<string, object> Elements { get; }
}