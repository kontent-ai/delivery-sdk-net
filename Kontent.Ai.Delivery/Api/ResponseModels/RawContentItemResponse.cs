using System.Text.Json.Serialization;

namespace Kontent.Ai.Delivery.Api.ResponseModels;

/// <summary>
/// Raw JSON response for a single content item from the Delivery API.
/// This represents the actual structure returned by Kontent.ai before any processing.
/// </summary>
internal sealed record RawContentItemResponse(
    [property: JsonPropertyName("item")] object Item,
    [property: JsonPropertyName("modular_content")] IDictionary<string, object>? ModularContent = null
);

/// <summary>
/// Raw JSON response for multiple content items from the Delivery API.
/// This represents the actual structure returned by Kontent.ai before any processing.
/// </summary>
internal sealed record RawContentItemListingResponse(
    [property: JsonPropertyName("items")] IList<object> Items,
    [property: JsonPropertyName("modular_content")] IDictionary<string, object>? ModularContent = null,
    [property: JsonPropertyName("pagination")] RawPagination? Pagination = null
);

/// <summary>
/// Raw pagination information from the Delivery API.
/// </summary>
internal sealed record RawPagination(
    [property: JsonPropertyName("skip")] int Skip = 0,
    [property: JsonPropertyName("limit")] int Limit = 0,
    [property: JsonPropertyName("count")] int Count = 0,
    [property: JsonPropertyName("next_page")] string? NextPage = null,
    [property: JsonPropertyName("total_count")] int? TotalCount = null
);

/// <summary>
/// Raw JSON response for content items feed from the Delivery API.
/// </summary>
internal sealed record RawContentItemsFeedResponse(
    [property: JsonPropertyName("items")] IList<object> Items,
    [property: JsonPropertyName("modular_content")] IDictionary<string, object>? ModularContent = null
);

/// <summary>
/// Raw JSON response for content types from the Delivery API.
/// </summary>
internal sealed record RawContentTypeResponse(
    [property: JsonPropertyName("type")] object Type
);

/// <summary>
/// Raw JSON response for multiple content types from the Delivery API.
/// </summary>
internal sealed record RawContentTypeListingResponse(
    [property: JsonPropertyName("types")] IList<object> Types,
    [property: JsonPropertyName("pagination")] RawPagination? Pagination = null
);

/// <summary>
/// Raw JSON response for a content type element from the Delivery API.
/// </summary>
internal sealed record RawContentElementResponse(
    [property: JsonPropertyName("element")] object Element
);

/// <summary>
/// Raw JSON response for taxonomy groups from the Delivery API.
/// </summary>
internal sealed record RawTaxonomyResponse(
    [property: JsonPropertyName("taxonomy")] object Taxonomy
);

/// <summary>
/// Raw JSON response for multiple taxonomy groups from the Delivery API.
/// </summary>
internal sealed record RawTaxonomyListingResponse(
    [property: JsonPropertyName("taxonomies")] IList<object> Taxonomies,
    [property: JsonPropertyName("pagination")] RawPagination? Pagination = null
);

/// <summary>
/// Raw JSON response for languages from the Delivery API.
/// </summary>
internal sealed record RawLanguageListingResponse(
    [property: JsonPropertyName("languages")] IList<object> Languages,
    [property: JsonPropertyName("pagination")] RawPagination? Pagination = null
);

/// <summary>
/// Raw JSON response for used-in queries from the Delivery API.
/// </summary>
internal sealed record RawUsedInResponse(
    [property: JsonPropertyName("items")] IList<object> Items
);