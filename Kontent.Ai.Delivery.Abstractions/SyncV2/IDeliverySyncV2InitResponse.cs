using System.Collections.Generic;

namespace Kontent.Ai.Delivery.Abstractions;

/// <summary>
/// Represents a response from Kontent.ai Sync API. Response includes continuation token for subsequent synchronization calls. Sync initialization should always return an empty list.
/// </summary>
public interface IDeliverySyncV2InitResponse : IResponse
{
    /// <summary>
    /// Gets list of delta update items.
    /// </summary>
    IList<ISyncV2Item> SyncItems { get; }

    /// <summary>
    /// Gets list of delta update types.
    /// </summary>
    IList<ISyncV2ContentType> SyncTypes { get; }

    /// <summary>
    /// Gets list of delta update taxonomies.
    /// </summary>
    IList<ISyncV2Taxonomy> SyncTaxonomies { get; }

    /// <summary>
    /// Gets list of delta update languages.
    /// </summary>
    IList<ISyncV2Language> SyncLanguages { get; }
}