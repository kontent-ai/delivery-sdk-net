namespace Kontent.Ai.Delivery.Api.QueryParams.Types;

/// <summary>
/// Query parameters for a single content type.
/// </summary>
internal sealed record SingleTypeParams
{
    /// <summary>
    /// The content elements to include in the response. By default, all elements are returned.
    /// </summary>
    public string[]? Elements { get; init; }
}