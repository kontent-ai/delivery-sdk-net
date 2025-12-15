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
/// <exception cref="ArgumentNullException">
/// Thrown when <paramref name="memoryCache"/> is null.
/// </exception>
public sealed class MemoryCacheManager(
    IMemoryCache memoryCache,
    string? keyPrefix = null,
    TimeSpan? defaultExpiration = null,
    ILogger<MemoryCacheManager>? logger = null) : IDeliveryCacheManager, IDisposable
{
    private readonly IMemoryCache _cache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
    private readonly string? _keyPrefix = keyPrefix;
    private readonly TimeSpan _defaultExpiration = defaultExpiration ?? TimeSpan.FromHours(1);
    private readonly ILogger<MemoryCacheManager>? _logger = logger;

    // Reverse index: dependency key -> set of cache keys that depend on it
    // Using ConcurrentDictionary for thread-safe access with HashSet for efficient lookups
    private readonly ConcurrentDictionary<string, HashSet<string>> _reverseIndex = new ConcurrentDictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

    // Locks for synchronizing reverse index updates per dependency key
    // This prevents race conditions when multiple threads update the same dependency
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _dependencyLocks = new ConcurrentDictionary<string, SemaphoreSlim>(StringComparer.OrdinalIgnoreCase);

    // Track all CancellationTokenSources for proper disposal
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _cancellationTokens = new ConcurrentDictionary<string, CancellationTokenSource>(StringComparer.OrdinalIgnoreCase);

    // Flag to track disposal
    private bool _disposed;

    // Treat null/empty prefix as "no prefix". Empty string can be used by callers to explicitly disable prefixing.
    private string KeyPrefixSegment => string.IsNullOrEmpty(_keyPrefix) ? "" : $"{_keyPrefix}:";

    /// <summary>
    /// Applies the key prefix to a cache key if one is configured.
    /// Thread-safe: uses readonly field initialized in constructor.
    /// </summary>
    private string PrefixKey(string key) => $"{KeyPrefixSegment}{key}";

    /// <inheritdoc />
    public Task<T?> GetAsync<T>(string cacheKey, CancellationToken cancellationToken = default)
        where T : class
    {
        ThrowIfDisposed();

        if (string.IsNullOrWhiteSpace(cacheKey))
        {
            // Return null for invalid keys rather than throwing
            // This aligns with the interface contract for cache misses
            return Task.FromResult<T?>(null);
        }

        try
        {
            // Check cancellation before cache operation
            cancellationToken.ThrowIfCancellationRequested();

            // Apply key prefix for cache isolation
            var prefixedKey = PrefixKey(cacheKey);

            // Attempt to retrieve from cache
            if (_cache.TryGetValue(prefixedKey, out var cached))
            {
                // Handle potential deserialization issues gracefully
                if (cached is T typedValue)
                {
                    return Task.FromResult<T?>(typedValue);
                }

                // Type mismatch or corruption - treat as cache miss
                // Remove the corrupted entry to prevent repeated failures
                _cache.Remove(prefixedKey);
            }
            else
            {
                // Best-effort cleanup: if the entry has expired but hasn't been scavenged yet,
                // removing it here triggers eviction callbacks which also clean up reverse indexes
                // and reverse index entries.
                _cache.Remove(prefixedKey);
            }

            return Task.FromResult<T?>(null);
        }
        catch (OperationCanceledException)
        {
            // Re-throw cancellation exceptions
            throw;
        }
        catch
        {
            // Treat any other exception as a cache miss
            // This ensures cache failures don't break the application
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

        // Validate inputs according to interface contract
        if (cacheKey == null)
            throw new ArgumentNullException(nameof(cacheKey));
        if (string.IsNullOrWhiteSpace(cacheKey))
            throw new ArgumentException("Cache key cannot be empty or whitespace.", nameof(cacheKey));
        if (value == null)
            throw new ArgumentNullException(nameof(value));
        if (dependencies == null)
            throw new ArgumentNullException(nameof(dependencies));

        cancellationToken.ThrowIfCancellationRequested();

        // Materialize dependencies to avoid multiple enumeration
        var dependencyList = dependencies as IList<string> ?? [.. dependencies];

        // Apply key prefix for cache isolation - use prefixed key consistently
        var prefixedCacheKey = PrefixKey(cacheKey);

        // Create cache entry options
        var entryOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration ?? _defaultExpiration,
            Priority = CacheItemPriority.Normal
        };

        // Create a CancellationTokenSource for this cache entry
        var entryCts = new CancellationTokenSource();

        // Register for cleanup when the entry is evicted
        entryOptions.RegisterPostEvictionCallback((key, value, reason, state) =>
        {
            // The 'is' pattern safely handles null - only enters block if key is non-null string
            if (key is string keyString)
            {
                // Log eviction
                if (_logger != null)
                    LoggerMessages.CacheEntryEvicted(_logger, keyString, reason.ToString());

                // Clean up reverse index when entry is evicted
                CleanupReverseIndexForKey(keyString);

                // Dispose the CancellationTokenSource
                if (_cancellationTokens.TryRemove(keyString, out var cts))
                {
                    cts.Dispose();
                }
            }
        });

        // Track dependencies in the reverse index
        // Note: Dependencies use prefixed keys to maintain consistency with cache keys
        var prefixedDependencies = dependencyList
            .Where(d => !string.IsNullOrWhiteSpace(d))
            .Select(PrefixKey)
            .ToList();

        foreach (var prefixedDependency in prefixedDependencies)
        {
            // Update reverse index with prefixed keys
            await UpdateReverseIndexAsync(prefixedDependency, prefixedCacheKey, cancellationToken).ConfigureAwait(false);
        }

        // Also link to the entry's own CancellationToken for direct invalidation
        entryOptions.AddExpirationToken(new CancellationChangeToken(entryCts.Token));

        // Store the CancellationTokenSource for this entry (use prefixed key for consistency)
        _cancellationTokens.TryAdd(prefixedCacheKey, entryCts);

        // Store in cache with prefixed key for isolation
        _cache.Set(prefixedCacheKey, value, entryOptions);

        // Log successful cache set
        if (_logger != null)
            LoggerMessages.CacheSetCompleted(_logger, cacheKey, dependencyList.Count);
    }

    /// <inheritdoc />
    public async Task InvalidateAsync(CancellationToken cancellationToken = default, params string[] dependencyKeys)
    {
        ThrowIfDisposed();

        if (dependencyKeys == null || dependencyKeys.Length == 0)
        {
            // No dependencies to invalidate - this is valid (idempotent)
            return;
        }

        var validKeys = dependencyKeys.Where(k => !string.IsNullOrWhiteSpace(k)).ToList();

        // Log invalidation start
        if (_logger != null && validKeys.Count > 0)
            LoggerMessages.CacheInvalidateStarting(_logger, validKeys.Count);

        // Process each dependency key with prefix applied for consistency
        var tasks = new List<Task>();

        foreach (var dependencyKey in validKeys)
        {
            tasks.Add(InvalidateDependencyAsync(PrefixKey(dependencyKey), dependencyKey, cancellationToken));
        }

        // Wait for all invalidations to complete
        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    /// <summary>
    /// Invalidates all cache entries that depend on the specified dependency key.
    /// </summary>
    private async Task InvalidateDependencyAsync(string dependencyKey, string originalKey, CancellationToken cancellationToken)
    {
        // Get the lock for this dependency to ensure thread-safe operations
        var lockObj = _dependencyLocks.GetOrAdd(dependencyKey, _ => new SemaphoreSlim(1, 1));

        await lockObj.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // Get all cache keys that depend on this dependency
            if (_reverseIndex.TryGetValue(dependencyKey, out var affectedKeys))
            {
                List<string> affectedKeysSnapshot;
                lock (affectedKeys)
                {
                    // Snapshot to avoid enumerating while eviction callbacks mutate the set.
                    affectedKeysSnapshot = affectedKeys.ToList();
                }

                // Also directly cancel each affected entry's CTS for immediate effect
                foreach (var cacheKey in affectedKeysSnapshot)
                {
                    if (_cancellationTokens.TryGetValue(cacheKey, out var entryCts))
                    {
                        entryCts.Cancel();
                    }
                }

                // Clean up the reverse index for this dependency
                _reverseIndex.TryRemove(dependencyKey, out _);
            }

            // Log invalidation completed
            if (_logger != null)
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
        var lockObj = _dependencyLocks.GetOrAdd(dependencyKey, _ => new SemaphoreSlim(1, 1));

        await lockObj.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var keys = _reverseIndex.GetOrAdd(
                dependencyKey,
                _ => new HashSet<string>(StringComparer.OrdinalIgnoreCase));

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
    /// Removes a cache key from all reverse index entries.
    /// </summary>
    /// <remarks>
    /// Called from cache eviction callbacks (synchronous). We must not wait on semaphores here:
    /// eviction callbacks can run while other operations hold locks, risking deadlocks/timeouts.
    /// </remarks>
    private void CleanupReverseIndexForKey(string cacheKey)
    {
        // Iterate through all dependencies and remove this cache key
        foreach (var kvp in _reverseIndex.ToArray()) // ToArray to avoid modification during enumeration
        {
            var dependencyKey = kvp.Key;
            // We intentionally avoid waiting on per-dependency semaphores here because eviction callbacks
            // can be invoked while other operations hold those semaphores (risking deadlocks/timeouts).
            // Reverse-index mutation is protected by locking the per-dependency HashSet instance.
            if (_reverseIndex.TryGetValue(dependencyKey, out var keys))
            {
                lock (keys)
                {
                    keys.Remove(cacheKey);

                    // If no more keys depend on this dependency, remove the entry
                    if (keys.Count == 0)
                    {
                        _reverseIndex.TryRemove(dependencyKey, out _);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Throws an ObjectDisposedException if this instance has been disposed.
    /// </summary>
    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(MemoryCacheManager));
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        // Dispose all entry CancellationTokenSources
        foreach (var cts in _cancellationTokens.Values)
        {
            cts?.Dispose();
        }
        _cancellationTokens.Clear();

        // Dispose all semaphores
        foreach (var semaphore in _dependencyLocks.Values)
        {
            semaphore?.Dispose();
        }
        _dependencyLocks.Clear();

        // Clear reverse index
        _reverseIndex.Clear();

        // Note: We don't dispose IMemoryCache as it's typically managed by DI container
    }
}
