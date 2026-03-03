namespace Kontent.Ai.Delivery.Abstractions;

/// <summary>
/// Identifies the source of a <see cref="IDeliveryResult{T}"/>.
/// </summary>
public enum ResponseSource
{
    /// <summary>
    /// The response was served directly from the Kontent.ai API origin server
    /// (or the <c>X-Cache</c> header was absent or indicated a MISS).
    /// </summary>
    Origin = 0,

    /// <summary>
    /// The response was served from the CDN edge cache (<c>X-Cache: HIT</c>).
    /// </summary>
    Cdn = 1,

    /// <summary>
    /// The response was served from the SDK's local cache (the cache factory was never invoked).
    /// </summary>
    Cache = 2,

    /// <summary>
    /// The response contains stale data served by the SDK cache's fail-safe mechanism
    /// after the cache factory (API call) failed or returned an error.
    /// </summary>
    FailSafe = 3
}
