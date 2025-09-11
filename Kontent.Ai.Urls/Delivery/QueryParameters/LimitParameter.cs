using Kontent.Ai.Delivery.Abstractions;

namespace Kontent.Ai.Urls.Delivery.QueryParameters;

/// <summary>
/// Specifies the maximum number of content items to return.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="LimitParameter"/> class using the specified limit.
/// </remarks>
/// <param name="limit">The maximum number of content items to return.</param>
public sealed class LimitParameter(int limit) : IQueryParameter
{
    /// <summary>
    /// Gets the maximum number of content items to return.
    /// </summary>
    public int Limit { get; } = limit;

    /// <summary>
    /// Returns the query string representation of the query parameter.
    /// </summary>
    public string GetQueryStringParameter()
    {
        return $"limit={Limit}";
    }
}
