using System.Collections.Generic;
using Kontent.Ai.Delivery.Abstractions;
using Newtonsoft.Json;
using Kontent.Ai.Delivery.SharedModels;

namespace Kontent.Ai.Delivery.Sync;

internal sealed class DeliverySyncResponse : AbstractResponse, IDeliverySyncResponse
{
    /// <inheritdoc/>
    public IList<ISyncItem> SyncItems { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DeliverySyncResponse"/>
    /// </summary>
    /// <param name="response"></param>
    /// <param name="syncItems"></param>
    [JsonConstructor]
    internal DeliverySyncResponse(IApiResponse response, IList<ISyncItem> syncItems)
        : base(response)
    {
        SyncItems = syncItems;
    }

    internal DeliverySyncResponse(IApiResponse response) 
        : base(response)
    {
    }
}