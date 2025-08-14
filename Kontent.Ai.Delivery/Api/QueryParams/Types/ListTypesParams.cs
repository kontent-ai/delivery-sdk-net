namespace Kontent.Ai.Delivery.Api.QueryParams.Types;

/// <summary>
/// Query parameters for listing content types.
/// </summary>
internal sealed record ListTypesParams
{
    /// <summary>
    /// The content elements to include in the response. By default, all elements are returned.
    /// </summary>
    public string[]? Elements { get; init; }

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
