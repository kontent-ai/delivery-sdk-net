using System.Text.Json.Serialization;

namespace Kontent.Ai.Delivery.Languages;

/// <inheritdoc cref="IDeliveryLanguageListingResponse" />
internal sealed record DeliveryLanguageListingResponse : IDeliveryLanguageListingResponse
{
    /// <inheritdoc/>
    [JsonPropertyName("languages")]
    public required IReadOnlyList<Language> Languages { get; init; }

    /// <inheritdoc/>
    [JsonPropertyName("pagination")]
    public required Pagination Pagination { get; init; }

    IReadOnlyList<ILanguage> IDeliveryLanguageListingResponse.Languages => Languages;
    IPagination IPageable.Pagination => Pagination;
}
