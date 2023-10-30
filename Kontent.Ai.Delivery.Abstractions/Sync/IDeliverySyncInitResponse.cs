using System.Collections.Generic;

namespace Kontent.Ai.Delivery.Abstractions;

/// <summary>
/// Represents a response from Kontent.ai Sync API. Response includes continuation token for subsequent synchronization calls. Sync initialization should always return an empty list.
/// </summary>
public interface IDeliverySyncInitResponse : IResponse
{
    /// <summary>
    /// Gets list of delta update items.
    /// </summary>
    IList<ISyncItem> SyncItems { get; }
}