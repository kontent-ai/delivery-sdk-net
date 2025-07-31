namespace Kontent.Ai.Delivery.Api.QueryParams.Base;

/// <summary>
/// Query parameter for including the total count of items.
/// </summary>
public interface IIncludeTotalCountParam
{
    /// <summary>
    /// Indicates whether to include the total count of items in the response.
    /// </summary>
    public bool? IncludeTotalCount { get; set; }
}
