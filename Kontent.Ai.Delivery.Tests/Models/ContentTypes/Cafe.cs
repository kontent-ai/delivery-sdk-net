using System.Text.Json.Serialization;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.ContentItems;
using Kontent.Ai.Delivery.SharedModels;

namespace Kontent.Ai.Delivery.Tests.Models.ContentTypes;

public record Cafe
{
    [JsonPropertyName("city")]
    public required string City { get; init; }

    [JsonPropertyName("country")]
    public required string Country { get; init; }

    [JsonPropertyName("email")]
    public required string Email { get; init; }

    [JsonPropertyName("phone")]
    public required string Phone { get; init; }

    [JsonPropertyName("photo")]
    public required IEnumerable<Asset> Photo { get; init; }

    [JsonPropertyName("sitemap")]
    public required IEnumerable<TaxonomyTerm> Sitemap { get; init; }

    [JsonPropertyName("state")]
    public required string State { get; init; }

    [JsonPropertyName("street")]
    public required string Street { get; init; }

    [JsonPropertyName("zip_code")]
    public required string ZipCode { get; init; }
}