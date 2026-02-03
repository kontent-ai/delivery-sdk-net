using System.Text.Json.Serialization;

namespace Kontent.Ai.Delivery.UsedIn;

internal sealed class DeliveryUsedInResponse : IDeliveryUsedInResponse
{
    /// <inheritdoc/>
    [JsonPropertyName("items")]
    public required IReadOnlyList<UsedInItem> Items { get; init; }

    IReadOnlyList<IUsedInItem> IDeliveryUsedInResponse.Items => Items;
}
