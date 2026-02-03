namespace Kontent.Ai.Delivery.Api.QueryParams.Items;

/// <summary>
/// Query parameters for a single content item.
/// </summary>
internal sealed record SingleItemParams
{
    /// <summary>
    /// Determines which language variant of content items to return. If not specified, the default language is used.
    /// </summary>
    public string? Language { get; init; }

    /// <summary>
    /// The content elements to include in the response. By default, all elements are returned.
    /// </summary>
    public string[]? Elements { get; init; }

    /// <summary>
    /// The content elements to exclude in the response. By default, all elements are returned.
    /// </summary>
    public string[]? ExcludeElements { get; init; }

    /// <summary>
    /// The depth of linked items to retrieve. If not specified, the default depth is 1.
    /// </summary>
    public int? Depth { get; init; }
}
