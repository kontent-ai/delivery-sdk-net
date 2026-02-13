using System.Collections.Concurrent;
using Kontent.Ai.Delivery.Logging;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace Kontent.Ai.Delivery.Caching;

/// <summary>
/// In-memory implementation of <see cref="IDeliveryCacheManager"/> using Microsoft.Extensions.Caching.Memory.
/// Provides thread-safe caching with automatic dependency-based invalidation.
/// </summary>
/// <remarks>
/// <para>
/// This implementation uses a dual-index architecture:
/// <list type="bullet">
/// <item><description>Primary cache: IMemoryCache for storing actual values</description></item>
/// <item><description>Reverse index: Maps dependency keys to cache entries that depend on them</description></item>
/// </list>
/// </para>
/// <para>
/// Invalidation is achieved through CancellationTokens. Each cache entry is associated with
/// a CancellationTokenSource that gets triggered when any of its dependencies are invalidated.
/// </para>
/// <para>
/// Thread-safety is ensured through:
/// <list type="bullet">
/// <item><description>ConcurrentDictionary for lock-free reverse index operations</description></item>
/// <item><description>Fine-grained locking per dependency key to prevent race conditions</description></item>
/// <item><description>IMemoryCache's inherent thread-safety</description></item>
/// </list>
/// </para>
/// </remarks>
/// <remarks>
/// Initializes a new instance of the <see cref="MemoryCacheManager"/> class.
/// </remarks>
/// <param name="memoryCache">The underlying memory cache instance.</param>
/// <param name="keyPrefix">
/// Optional prefix for all cache keys. Used to isolate cache entries when multiple clients share the same IMemoryCache.
/// For example, "production" or "preview:myproject".
/// </param>
/// <param name="defaultExpiration">
/// Default expiration time for cache entries. If null, defaults to 1 hour.
/// </param>
/// <param name="logger"></param>
/// <exception cref="ArgumentNullException">
/// Thrown when <paramref name="memoryCache"/> is null.
/// </exception>
internal sealed class MemoryCacheManager(
    IMemoryCache memoryCache,
    string? keyPrefix = null,
    TimeSpan? defaultExpiration = null,
    ILogger<MemoryCacheManager>? logger = null) : IDeliveryCacheManager, IDeliveryCachePurger, IDisposable
{
    private readonly IMemoryCache _cache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
    private readonly string? _keyPrefix = keyPrefix;
    private readonly TimeSpan _defaultExpiration = defaultExpiration ?? TimeSpan.FromHours(1);
    private readonly ILogger<MemoryCacheManager>? _logger = logger;
    private readonly ConcurrentDictionary<string, HashSet<string>> _reverseIndex = new(StringComparer.OrdinalIgnoreCase);
    private const int LockStripeCount = 64;
    private readonly SemaphoreSlim[] _reverseIndexLocks = CreateLockStripes(LockStripeCount);
    private readonly ConcurrentDictionary<string, CacheEntryMetadata> _entries = new(StringComparer.Ordinal);

    private sealed record CacheEntryMetadata(CancellationTokenSource Cts, string[] Dependencies);
    private bool _disposed;

    // Global purge token: all cache entries link to this token so PurgeAsync can invalidate everything efficiently.
    private readonly object _purgeLock = new();
    private CancellationTokenSource _purgeCts = new();
    private string KeyPrefixSegment => string.IsNullOrEmpty(_keyPrefix) ? "" : $"{_keyPrefix}:";
    private string PrefixKey(string key) => $"{KeyPrefixSegment}{key}";

    private static SemaphoreSlim[] CreateLockStripes(int count)
    {
        var locks = new SemaphoreSlim[count];
        for (var i = 0; i < locks.Length; i++)
        {
            locks[i] = new SemaphoreSlim(1, 1);
        }
        return locks;
    }

    /// <summary>
    /// Gets the lock stripe for the given dependency key by using value of lower 6 bits of the hash code (0-63).
    /// </summary>
    private SemaphoreSlim GetLockStripe(string dependencyKey)
    {
        var hash = StringComparer.OrdinalIgnoreCase.GetHashCode(dependencyKey);
        var index = hash & (LockStripeCount - 1);
        return _reverseIndexLocks[index];
    }

    /// <inheritdoc />
    public Task<T?> GetAsync<T>(string cacheKey, CancellationToken cancellationToken = default)
        where T : class
    {
        ThrowIfDisposed();

        if (string.IsNullOrWhiteSpace(cacheKey))
        {
            return Task.FromResult<T?>(null);
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var prefixedKey = PrefixKey(cacheKey);

            if (_cache.TryGetValue(prefixedKey, out var cached) && cached is T typedValue)
                return Task.FromResult<T?>(typedValue);

            // Best-effort cleanup: if the entry has expired but hasn't been scavenged yet,
            // removing it here triggers eviction callbacks which also clean up reverse indexes
            // and reverse index entries.
            _cache.Remove(prefixedKey);

            return Task.FromResult<T?>(null);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return Task.FromResult<T?>(null);
        }
    }

    /// <inheritdoc />
    public async Task SetAsync<T>(
        string cacheKey,
        T value,
        IEnumerable<string> dependencies,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default) where T : class
    {
        ThrowIfDisposed();

        ArgumentNullException.ThrowIfNull(cacheKey);
        if (string.IsNullOrWhiteSpace(cacheKey))
            throw new ArgumentException("Cache key cannot be empty or whitespace.", nameof(cacheKey));
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(dependencies);

        cancellationToken.ThrowIfCancellationRequested();

        var dependencyList = dependencies as IList<string> ?? [.. dependencies];
        var prefixedCacheKey = PrefixKey(cacheKey);

        var prefixedDependencies = dependencyList
            .Where(d => !string.IsNullOrWhiteSpace(d))
            .Select(PrefixKey)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var entryCts = new CancellationTokenSource();
        var entryMetadata = new CacheEntryMetadata(entryCts, prefixedDependencies);
        var entryOptions = CreateCacheEntryOptions(expiration, entryMetadata, entryCts);

        foreach (var prefixedDependency in prefixedDependencies)
        {
            await UpdateReverseIndexAsync(prefixedDependency, prefixedCacheKey, cancellationToken).ConfigureAwait(false);
        }

        _entries.AddOrUpdate(prefixedCacheKey, entryMetadata, (_, _) => entryMetadata);

        _cache.Set(prefixedCacheKey, value, entryOptions);

        if (_logger is not null)
            LoggerMessages.CacheSetCompleted(_logger, cacheKey, dependencyList.Count);
    }

    /// <inheritdoc />
    public async Task InvalidateAsync(CancellationToken cancellationToken = default, params string[] dependencyKeys)
    {
        ThrowIfDisposed();

        if (dependencyKeys is null || dependencyKeys.Length == 0)
        {
            return;
        }

        var validKeys = dependencyKeys.Where(k => !string.IsNullOrWhiteSpace(k)).ToList();

        if (_logger is not null && validKeys.Count > 0)
            LoggerMessages.CacheInvalidateStarting(_logger, validKeys.Count);

        var tasks = new List<Task>();

        foreach (var dependencyKey in validKeys)
        {
            tasks.Add(InvalidateDependencyAsync(PrefixKey(dependencyKey), dependencyKey, cancellationToken));
        }

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task PurgeAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();

        CancellationTokenSource toCancel;
        lock (_purgeLock)
        {
            toCancel = _purgeCts;
            _purgeCts = new CancellationTokenSource();
        }

        try
        {
            toCancel.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // Best-effort; never throw from purge.
        }
        finally
        {
            try
            {
                toCancel.Dispose();
            }
            catch
            {
                // Best-effort
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Invalidates all cache entries that depend on the specified dependency key.
    /// </summary>
    private async Task InvalidateDependencyAsync(string dependencyKey, string originalKey, CancellationToken cancellationToken)
    {
        var lockObj = GetLockStripe(dependencyKey);

        await lockObj.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_reverseIndex.TryGetValue(dependencyKey, out var affectedKeys))
            {
                List<string> affectedKeysSnapshot;
                lock (affectedKeys)
                {
                    // Snapshot to avoid enumerating while eviction callbacks mutate the set.
                    affectedKeysSnapshot = [.. affectedKeys];

                    // Keep the reverse-index entry, but clear its current contents.
                    // This avoids races where concurrent SetAsync would add to a HashSet instance that
                    // is no longer referenced by the dictionary (which would happen if we removed it).
                    affectedKeys.Clear();
                }

                TryCancelAffectedEntries(affectedKeysSnapshot, dependencyKey);
            }

            if (_logger is not null)
                LoggerMessages.CacheInvalidateCompleted(_logger, originalKey);
        }
        finally
        {
            lockObj.Release();
        }
    }

    /// <summary>
    /// Updates the reverse index to track that a cache key depends on a dependency.
    /// </summary>
    private async Task UpdateReverseIndexAsync(
        string dependencyKey,
        string cacheKey,
        CancellationToken cancellationToken)
    {
        var lockObj = GetLockStripe(dependencyKey);

        await lockObj.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var keys = _reverseIndex.GetOrAdd(
                dependencyKey,
                _ => new HashSet<string>(StringComparer.Ordinal));

            lock (keys)
            {
                keys.Add(cacheKey);
            }
        }
        finally
        {
            lockObj.Release();
        }
    }

    /// <summary>
    /// Attempts to cancel all affected cache entries that still depend on the specified dependency key.
    /// </summary>
    /// <remarks>
    /// Guards against stale reverse-index associations by verifying each entry still depends on the dependency.
    /// </remarks>
    private void TryCancelAffectedEntries(List<string> affectedKeys, string dependencyKey)
    {
        foreach (var cacheKey in affectedKeys)
        {
            if (!_entries.TryGetValue(cacheKey, out var metadata))
                continue;

            if (!metadata.Dependencies.Contains(dependencyKey, StringComparer.OrdinalIgnoreCase))
                continue;

            try
            {
                metadata.Cts.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // Entry already evicted/disposed; ignore
            }
        }
    }

    /// <summary>
    /// Creates cache entry options with eviction handling and expiration tokens.
    /// </summary>
    private MemoryCacheEntryOptions CreateCacheEntryOptions(
        TimeSpan? expiration,
        CacheEntryMetadata entryMetadata,
        CancellationTokenSource entryCts)
    {
        var entryOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration ?? _defaultExpiration,
            Priority = CacheItemPriority.Normal
        };

        entryOptions.RegisterPostEvictionCallback((key, value, reason, state) =>
        {
            if (key is string keyString && state is CacheEntryMetadata evictedMetadata)
                HandleCacheEntryEviction(keyString, evictedMetadata, reason);
        }, state: entryMetadata);

        entryOptions.AddExpirationToken(new CancellationChangeToken(entryCts.Token));

        // Link to the global purge token for O(1) invalidation of all entries
        lock (_purgeLock)
        {
            entryOptions.AddExpirationToken(new CancellationChangeToken(_purgeCts.Token));
        }

        return entryOptions;
    }

    /// <summary>
    /// Handles cache entry eviction by cleaning up reverse index and metadata.
    /// </summary>
    /// <remarks>
    /// Called from eviction callbacks. Must be safe for concurrent/repeated calls.
    /// </remarks>
    private void HandleCacheEntryEviction(string keyString, CacheEntryMetadata evictedMetadata, EvictionReason reason)
    {
        if (_logger is not null)
            LoggerMessages.CacheEntryEvicted(_logger, keyString, reason.ToString());

        CleanupReverseIndexForKey(keyString, evictedMetadata);

        if (_entries.TryGetValue(keyString, out var current) &&
            ReferenceEquals(current.Cts, evictedMetadata.Cts))
        {
            _entries.TryRemove(keyString, out _);
        }

        try
        {
            evictedMetadata.Cts.Dispose();
        }
        catch
        {
            // Best-effort cleanup; never throw from eviction callbacks
        }
    }

    /// <summary>
    /// Removes a cache key from all reverse index entries.
    /// </summary>
    /// <remarks>
    /// Called from cache eviction callbacks (synchronous). We must not wait on semaphores here:
    /// eviction callbacks can run while other operations hold locks, risking deadlocks/timeouts.
    /// </remarks>
    private void CleanupReverseIndexForKey(string cacheKey, CacheEntryMetadata evictedMetadata)
    {
        foreach (var dependencyKey in evictedMetadata.Dependencies)
        {
            if (_reverseIndex.TryGetValue(dependencyKey, out var keys))
            {
                lock (keys)
                {
                    if (_entries.TryGetValue(cacheKey, out var current) &&
                        !ReferenceEquals(current.Cts, evictedMetadata.Cts) &&
                        current.Dependencies.Contains(dependencyKey, StringComparer.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    keys.Remove(cacheKey);

                    if (keys.Count == 0)
                    {
                        _reverseIndex.TryRemove(dependencyKey, out _);
                    }
                }
            }
        }
    }

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, nameof(MemoryCacheManager));

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        try
        { _purgeCts.Cancel(); }
        catch { /* Best-effort */ }
        try
        { _purgeCts.Dispose(); }
        catch { /* Best-effort */ }

        foreach (var entry in _entries.Values)
        {
            try
            {
                entry?.Cts.Dispose();
            }
            catch
            {
                // Best-effort
            }
        }
        _entries.Clear();

        foreach (var semaphore in _reverseIndexLocks)
        {
            try
            {
                semaphore.Dispose();
            }
            catch
            {
                // Best-effort
            }
        }

        _reverseIndex.Clear();
    }
}
