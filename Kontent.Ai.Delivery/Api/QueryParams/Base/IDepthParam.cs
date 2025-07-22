namespace Kontent.Ai.Delivery.Api.QueryParams.Base;

/// <summary>
/// Query parameter for linked items depth.
/// </summary>
public interface IDepthParam
{
    /// <summary>
    /// The depth of linked items to retrieve. If not specified, the default depth is 1.
    /// </summary>
    public int? Depth { get; set; }
}

