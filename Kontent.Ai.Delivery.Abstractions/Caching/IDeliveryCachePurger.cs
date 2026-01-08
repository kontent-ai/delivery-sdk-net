namespace Kontent.Ai.Delivery.Abstractions;

/// <summary>
/// Optional capability interface for cache managers that can purge (invalidate) all entries at once.
/// </summary>
/// <remarks>
/// <para>
/// Not all cache backends can support purging all entries (e.g., generic <c>IDistributedCache</c>
/// does not provide key enumeration). This interface is intentionally separate from
/// <see cref="IDeliveryCacheManager"/> to avoid forcing unsupported operations.
/// </para>
/// <para>
/// Implementations should only purge entries managed by the specific cache manager instance
/// (including its configured key prefix/namespace).
/// </para>
/// </remarks>
public interface IDeliveryCachePurger
{
    /// <summary>
    /// Purges (invalidates) all cache entries managed by this cache manager.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task PurgeAsync(CancellationToken cancellationToken = default);
}

