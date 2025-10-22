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
}