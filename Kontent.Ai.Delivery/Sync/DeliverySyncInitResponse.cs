using System.Collections.Generic;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.SharedModels;
using Newtonsoft.Json;

namespace Kontent.Ai.Delivery.Sync;

internal sealed class DeliverySyncInitResponse : AbstractResponse, IDeliverySyncInitResponse
{
    /// <inheritdoc/>
    public IList<ISyncItem> SyncItems { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DeliverySyncInitResponse"/>
    /// </summary>
    /// <param name="response"></param>
    /// <param name="syncItems"></param>
    [JsonConstructor]
    public DeliverySyncInitResponse(IApiResponse response, IList<ISyncItem> syncItems)
        : base(response)
    {
        SyncItems = syncItems;
    }

    public DeliverySyncInitResponse(IApiResponse response) : base(response)
    {
    }
}