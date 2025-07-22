namespace Kontent.Ai.Delivery.Api.QueryParams.Base;

/// <summary>
/// Query parameters for paging.
/// </summary>
public interface IPagingParams
{
    /// <summary>
    /// The number of items to skip.
    /// </summary>
    public int? Skip { get; set; }

    /// <summary>
    /// The maximum number of items to return per request.
    /// </summary>
    public int? Limit { get; set; }
}
