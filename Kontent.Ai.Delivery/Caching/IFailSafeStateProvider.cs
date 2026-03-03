namespace Kontent.Ai.Delivery.Caching;

/// <summary>
/// Internal capability interface for cache managers that can report whether
/// a given SDK cache key is currently served via fail-safe stale data.
/// </summary>
internal interface IFailSafeStateProvider
{
    /// <summary>
    /// Returns <c>true</c> when fail-safe is active for the specified SDK cache key.
    /// </summary>
    /// <param name="cacheKey">SDK cache key (unformatted, before prefixes).</param>
    bool IsFailSafeActive(string cacheKey);
}
