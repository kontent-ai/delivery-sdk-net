using Refit;

namespace Kontent.Ai.Delivery.Api.QueryParams.Base;

/// <summary>
/// Query parameter for ordering items listing by a specified field.
/// </summary>
public interface IOrderingParam
{
    /// <summary>
    /// Orders the items by the specified field in ascending or descending order.
    /// By default, the items are ordered alphabetically by codename.
    /// </summary>
    [AliasAs("order")]
    public string? OrderBy { get; set; }
}
