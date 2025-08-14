namespace Kontent.Ai.Delivery.Api.QueryParams.Languages;

/// <summary>
/// Query parameters for listing languages.
/// </summary>
internal sealed record LanguagesParams
{
    /// <summary>
    /// Orders the items by the specified field in ascending or descending order.
    /// By default, the items are ordered alphabetically by codename.
    /// </summary>
    [AliasAs("order")]
    public string? OrderBy { get; init; }

    /// <summary>
    /// The number of items to skip.
    /// </summary>
    public int? Skip { get; init; }

    /// <summary>
    /// The maximum number of items to return per request.
    /// </summary>
    public int? Limit { get; init; }
}
