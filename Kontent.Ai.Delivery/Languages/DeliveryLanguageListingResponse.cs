using System.Text.Json.Serialization;

namespace Kontent.Ai.Delivery.Languages;

/// <inheritdoc cref="IDeliveryLanguageListingResponse" />
internal sealed record DeliveryLanguageListingResponse : IDeliveryLanguageListingResponse
{
    /// <inheritdoc/>
    [JsonPropertyName("languages")]
    public IList<Language>? Languages
    {
        get; init;
    }

    /// <inheritdoc/>
    [JsonPropertyName("pagination")]
    public Pagination? Pagination
    {
        get; init;
    }

    IList<ILanguage> IDeliveryLanguageListingResponse.Languages => [.. Languages.Cast<ILanguage>()];

    IPagination IPageable.Pagination => Pagination;
}
