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
    public required IReadOnlyList<ContentType> Types { get; init; }

    IReadOnlyList<IContentType> IDeliveryTypeListingResponse.Types => Types;
    IPagination IPageable.Pagination => Pagination;
}