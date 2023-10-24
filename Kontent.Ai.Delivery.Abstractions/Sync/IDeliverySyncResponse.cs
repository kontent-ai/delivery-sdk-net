using System.Collections.Generic;

namespace Kontent.Ai.Delivery.Abstractions;

/// <summary>
/// Represents a response from Kontent.ai Sync API that contains recently updated items. Response includes continuation token for subsequent synchronization calls.
/// </summary>
public interface IDeliverySyncResponse : IResponse
{
    /// <summary>
    /// Gets the list of items delta updates.
    /// </summary>
    IList<ISyncItem> SyncItems { get; }
}