using System.Collections.Generic;
using Kontent.Ai.Delivery.Abstractions;
using Newtonsoft.Json;
using Kontent.Ai.Delivery.SharedModels;

namespace Kontent.Ai.Delivery.SyncV2;

internal sealed class DeliverySyncV2Response : AbstractResponse, IDeliverySyncV2Response
{
    /// <inheritdoc/>
    public IList<ISyncV2Item> SyncItems { get; }

    /// <inheritdoc/>
    public IList<ISyncV2ContentType> SyncTypes { get; }

    /// <inheritdoc/>
    public IList<ISyncV2Taxonomy> SyncTaxonomies { get; }

    /// <inheritdoc/>
    public IList<ISyncV2Language> SyncLanguages { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DeliverySyncV2Response"/>
    /// </summary>
    /// <param name="response"></param>
    /// <param name="syncItems"></param>
    /// <param name="syncTypes"></param>
    /// <param name="syncTaxonomies"></param>
    /// <param name="syncLanguages"></param>
    [JsonConstructor]
    internal DeliverySyncV2Response(
        IApiResponse response,
        IList<ISyncV2Item> syncItems,
        IList<ISyncV2ContentType> syncTypes,
        IList<ISyncV2Taxonomy> syncTaxonomies,
        IList<ISyncV2Language> syncLanguages)
        : base(response)
    {
        SyncItems = syncItems;
        SyncTypes = syncTypes;
        SyncTaxonomies = syncTaxonomies;
        SyncLanguages = syncLanguages;
    }

    internal DeliverySyncV2Response(IApiResponse response)
        : base(response)
    {
    }
}