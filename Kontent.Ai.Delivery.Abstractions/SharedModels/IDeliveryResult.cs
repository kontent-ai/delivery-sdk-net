using System.Net;
using System.Net.Http.Headers;

namespace Kontent.Ai.Delivery.Abstractions;

/// <summary>
/// Represents the result of a Delivery API operation.
/// </summary>
/// <typeparam name="T">The type of the result value.</typeparam>
public interface IDeliveryResult<out T>
{
    /// <summary>
    /// Gets the result value when the operation was successful.
    /// </summary>
    T Value { get; }

    /// <summary>
    /// Gets a value indicating whether the operation was successful.
    /// </summary>
    bool IsSuccess { get; }

    /// <summary>
    /// Gets the error that occurred during the operation.
    /// </summary>
    IError? Error { get; }

    /// <summary>
    /// Gets the HTTP status code of the response.
    /// </summary>
    HttpStatusCode StatusCode { get; }

    /// <summary>
    /// Gets a value indicating whether the content is stale.
    /// Stale content indicates that there is a more recent version, but it will become available later.
    /// </summary>
    bool HasStaleContent { get; }

    /// <summary>
    /// Gets the continuation token for pagination, if applicable.
    /// </summary>
    string? ContinuationToken { get; }

    /// <summary>
    /// Gets the URL used to retrieve this response for debugging purposes.
    /// </summary>
    string? RequestUrl { get; }

    /// <summary>
    /// Gets the HTTP response headers from the Delivery API.
    /// Returns <c>null</c> when the result is served from SDK cache (<see cref="IsCacheHit"/> is <c>true</c>).
    /// </summary>
    HttpResponseHeaders? ResponseHeaders { get; }

    /// <summary>
    /// Gets a value indicating whether this result was served from the SDK's local cache
    /// (MemoryCacheManager or DistributedCacheManager).
    /// When <c>true</c>, <see cref="ResponseHeaders"/> will be <c>null</c> and properties like
    /// <see cref="StatusCode"/>, <see cref="HasStaleContent"/>, and <see cref="ContinuationToken"/>
    /// contain synthetic values.
    /// </summary>
    /// <remarks>
    /// This is distinct from CDN-level caching (e.g., Fastly). To check for CDN cache hits,
    /// inspect the <see cref="ResponseHeaders"/> for headers like <c>X-Cache</c>.
    /// </remarks>
    bool IsCacheHit { get; }
}
