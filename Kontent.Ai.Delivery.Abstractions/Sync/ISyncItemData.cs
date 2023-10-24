using System.Collections.Generic;

namespace Kontent.Ai.Delivery.Abstractions;

/// <summary>
/// Represents a delta update.
/// </summary>
public interface ISyncItemData
{
    IContentItemSystemAttributes System { get; }
    object Elements { get; }
}