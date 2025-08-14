namespace Kontent.Ai.Delivery.Api.QueryParams.TaxonomyGroups;

/// <summary>
/// Query parameters for listing taxonomy groups.
/// </summary>
internal sealed record ListTaxonomyGroupsParams
{
    /// <summary>
    /// The number of items to skip.
    /// </summary>
    public int? Skip { get; init; }

    /// <summary>
    /// The maximum number of items to return per request.
    /// </summary>
    public int? Limit { get; init; }

    /// <summary>
    /// Filtering parameters in the format expected by the Delivery API.
    /// </summary>
    public string[]? Filters { get; init; }
}
