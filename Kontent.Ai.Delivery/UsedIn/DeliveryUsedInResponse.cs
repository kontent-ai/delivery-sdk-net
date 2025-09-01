using System.Text.Json.Serialization;

namespace Kontent.Ai.Delivery.UsedIn;

internal sealed class DeliveryUsedInResponse : IDeliveryItemsFeedResponse<IUsedInItem>
{
    /// <inheritdoc/>
    public IList<IUsedInItem> Items { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DeliveryUsedInResponse"/>
    /// </summary>
    /// <param name="items"></param>
    [JsonConstructor]
    internal DeliveryUsedInResponse(IList<IUsedInItem> items)
    {
        Items = items;
    }
}