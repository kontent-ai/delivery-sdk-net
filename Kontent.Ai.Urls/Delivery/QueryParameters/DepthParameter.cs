using Kontent.Ai.Delivery.Abstractions;

namespace Kontent.Ai.Urls.Delivery.QueryParameters;

/// <summary>
/// Specifies the maximum level of recursion when retrieving linked items. If not specified, the default depth is one level.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="DepthParameter"/> class using the specified depth level.
/// </remarks>
/// <param name="depth">The maximum level of recursion to use when retrieving linked items.</param>
public sealed class DepthParameter(int depth) : IQueryParameter
{
    /// <summary>
    /// Gets the maximum level of recursion when retrieving linked items.
    /// </summary>
    public int Depth { get; } = depth;

    /// <summary>
    /// Returns the query string representation of the query parameter.
    /// </summary>
    public string GetQueryStringParameter()
    {
        return $"depth={Depth}";
    }
}
