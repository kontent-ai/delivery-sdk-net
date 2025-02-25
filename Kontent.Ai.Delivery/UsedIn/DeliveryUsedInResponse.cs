using System.Collections.Generic;
using Kontent.Ai.Delivery.Abstractions;
using Newtonsoft.Json;
using Kontent.Ai.Delivery.SharedModels;

namespace Kontent.Ai.Delivery.UsedIn;

internal sealed class DeliveryUsedInResponse : AbstractResponse, IDeliveryItemsFeedResponse<IUsedInItem>
{
    /// <inheritdoc/>
    public IList<IUsedInItem> Items { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DeliveryUsedInResponse"/>
    /// </summary>
    /// <param name="response"></param>
    /// <param name="items"></param>
    [JsonConstructor]
    internal DeliveryUsedInResponse(IApiResponse response, IList<IUsedInItem> items)
        : base(response)
    {
        Items = items;
    }

    internal DeliveryUsedInResponse(IApiResponse response)
        : base(response)
    {
    }
}