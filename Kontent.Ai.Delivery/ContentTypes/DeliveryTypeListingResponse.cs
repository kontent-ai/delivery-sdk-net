using System.Text.Json.Serialization;

namespace Kontent.Ai.Delivery.ContentTypes;

/// <inheritdoc cref="IDeliveryTypeListingResponse" />
internal sealed record DeliveryTypeListingResponse : IDeliveryTypeListingResponse
{
    /// <inheritdoc/>
    [JsonPropertyName("pagination")]
    public required Pagination Pagination { get; init; }

    /// <inheritdoc/>
    [JsonPropertyName("types")]
    public required IList<ContentType> Types { get; init; }

    IList<IContentType> IDeliveryTypeListingResponse.Types => [.. Types.Cast<IContentType>()];

    IPagination IPageable.Pagination => Pagination;
}
