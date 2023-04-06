using System.Collections.Generic;

namespace Kontent.Ai.Delivery.Abstractions;

/// <summary>
/// Represents a response from Kontent.ai Delivery API that contains a taxonomy group.
/// </summary>
public interface IDeliverySyncResponse : IResponse
{
    /// <summary>
    /// Gets list of delta update items.
    /// </summary>
    IList<ISyncItem> SyncItems { get; }
}