using System.Collections.Generic;

namespace Kontent.Ai.Delivery.Abstractions;

/// <summary>
/// Represents a response from Kontent.ai Sync API that contains recently updated items. Response includes continuation token for subsequent synchronization calls.
/// </summary>
public interface IDeliverySyncV2Response : IResponse
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