namespace Kontent.Ai.Delivery.Api.QueryParams.Items;

/// <summary>
/// Query parameters for content item listing.
/// </summary>
internal sealed record ListItemsParams
{
    /// <summary>
    /// Determines which language variant of content items to return. If not specified, the default language is used.
    /// </summary>
    public string? Language { get; init; }

    /// <summary>
    /// The content elements to include in the response. By default, all elements are returned.
    /// </summary>
    public string? Elements { get; init; }

    /// <summary>
    /// The content elements to exclude in the response. By default, all elements are returned.
    /// </summary>
    public string? ExcludeElements { get; init; }

    /// <summary>
    /// The depth of linked items to retrieve. If not specified, the default depth is 1.
    /// </summary>
    public int? Depth { get; init; }

    /// <summary>
    /// The number of items to skip.
    /// </summary>
    public int? Skip { get; init; }

    /// <summary>
    /// The maximum number of items to return per request.
    /// </summary>
    public int? Limit { get; init; }

    /// <summary>
    /// Orders the items by the specified field in ascending or descending order.
    /// By default, the items are ordered alphabetically by codename.
    /// </summary>
    [AliasAs("order")]
    public string? OrderBy { get; init; }

    /// <summary>
    /// Indicates whether to include the total count of items in the response.
    /// </summary>
    public bool? IncludeTotalCount { get; init; }
}
