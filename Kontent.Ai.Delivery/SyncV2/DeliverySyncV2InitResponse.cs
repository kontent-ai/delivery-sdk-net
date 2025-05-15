using System.Collections.Generic;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.SharedModels;
using Newtonsoft.Json;

namespace Kontent.Ai.Delivery.SyncV2;

internal sealed class DeliverySyncV2InitResponse : AbstractResponse, IDeliverySyncV2InitResponse
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
    /// Initializes a new instance of the <see cref="DeliverySyncV2InitResponse"/>
    /// </summary>
    /// <param name="response"></param>
    /// <param name="syncItems"></param>
    /// <param name="syncTypes"></param>
    /// <param name="syncTaxonomies"></param>
    /// <param name="syncLanguages"></param>
    [JsonConstructor]
    public DeliverySyncV2InitResponse(
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

    public DeliverySyncV2InitResponse(IApiResponse response) : base(response)
    {
    }
}
