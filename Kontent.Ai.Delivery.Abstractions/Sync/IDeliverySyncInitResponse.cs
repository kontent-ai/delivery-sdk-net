using System.Collections.Generic;

namespace Kontent.Ai.Delivery.Abstractions;

/// <summary>
/// Represents a response from Kontent.ai Delivery API that contains a continuation token and .
/// </summary>
public interface IDeliverySyncInitResponse : IResponse
{
    /// <summary>
    /// Gets list of delta update items.
    /// </summary>
    IList<ISyncItem> SyncItems { get; }
}