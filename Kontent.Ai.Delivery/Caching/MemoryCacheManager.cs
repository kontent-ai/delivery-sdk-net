using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;
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
public sealed class MemoryCacheManager : IDeliveryCacheManager, IDisposable
{
    private readonly IMemoryCache _cache;
    private readonly TimeSpan _defaultExpiration;

    // Reverse index: dependency key -> set of cache keys that depend on it
    // Using ConcurrentDictionary for thread-safe access with HashSet for efficient lookups
    private readonly ConcurrentDictionary<string, HashSet<string>> _reverseIndex;

    // Locks for synchronizing reverse index updates per dependency key
    // This prevents race conditions when multiple threads update the same dependency
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _dependencyLocks;

    // Track all CancellationTokenSources for proper disposal
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _cancellationTokens;

    // Track dependency CancellationTokenSources separately for proper lifecycle management
    // These are stored in both _cache and this dictionary to ensure cleanup when evicted
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _dependencyCancellationTokens;

    // Prefix for storing CancellationTokenSource in cache (to differentiate from actual values)
    private const string CancellationTokenPrefix = "__cts:";

    // Flag to track disposal
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryCacheManager"/> class.
    /// </summary>
    /// <param name="memoryCache">The underlying memory cache instance.</param>
    /// <param name="defaultExpiration">
    /// Default expiration time for cache entries. If null, defaults to 1 hour.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="memoryCache"/> is null.
    /// </exception>
    public MemoryCacheManager(IMemoryCache memoryCache, TimeSpan? defaultExpiration = null)
    {
        _cache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        _defaultExpiration = defaultExpiration ?? TimeSpan.FromHours(1);
        _reverseIndex = new ConcurrentDictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        _dependencyLocks = new ConcurrentDictionary<string, SemaphoreSlim>(StringComparer.OrdinalIgnoreCase);
        _cancellationTokens = new ConcurrentDictionary<string, CancellationTokenSource>(StringComparer.OrdinalIgnoreCase);
        _dependencyCancellationTokens = new ConcurrentDictionary<string, CancellationTokenSource>(StringComparer.OrdinalIgnoreCase);
    }

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

            // Attempt to retrieve from cache
            if (_cache.TryGetValue(cacheKey, out var cached))
            {
                // Handle potential deserialization issues gracefully
                if (cached is T typedValue)
                {
                    return Task.FromResult(typedValue);
                }

                // Type mismatch or corruption - treat as cache miss
                // Remove the corrupted entry to prevent repeated failures
                _cache.Remove(cacheKey);
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
                // Clean up reverse index when entry is evicted
                CleanupReverseIndexForKey(keyString);

                // Dispose the CancellationTokenSource
                if (_cancellationTokens.TryRemove(keyString, out var cts))
                {
                    cts.Dispose();
                }
            }
        });

        // Link dependencies using CancellationTokens
        foreach (var dependency in dependencyList.Where(d => !string.IsNullOrWhiteSpace(d)))
        {
            // Get or create a CancellationTokenSource for this dependency
            var dependencyCts = await GetOrCreateDependencyCancellationTokenAsync(
                dependency,
                cancellationToken).ConfigureAwait(false);

            if (dependencyCts != null)
            {
                // Link the dependency's cancellation to this entry's eviction
                entryOptions.AddExpirationToken(new CancellationChangeToken(dependencyCts.Token));

                // Update reverse index
                await UpdateReverseIndexAsync(dependency, cacheKey, cancellationToken).ConfigureAwait(false);
            }
        }

        // Also link to the entry's own CancellationToken for direct invalidation
        entryOptions.AddExpirationToken(new CancellationChangeToken(entryCts.Token));

        // Store the CancellationTokenSource for this entry
        _cancellationTokens.TryAdd(cacheKey, entryCts);

        // Store in cache
        _cache.Set(cacheKey, value, entryOptions);
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

        // Process each dependency key
        var tasks = new List<Task>();

        foreach (var dependencyKey in dependencyKeys.Where(k => !string.IsNullOrWhiteSpace(k)))
        {
            tasks.Add(InvalidateDependencyAsync(dependencyKey, cancellationToken));
        }

        // Wait for all invalidations to complete
        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    /// <summary>
    /// Invalidates all cache entries that depend on the specified dependency key.
    /// </summary>
    private async Task InvalidateDependencyAsync(string dependencyKey, CancellationToken cancellationToken)
    {
        // Get the lock for this dependency to ensure thread-safe operations
        var lockObj = _dependencyLocks.GetOrAdd(dependencyKey, _ => new SemaphoreSlim(1, 1));

        await lockObj.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // Cancel the dependency's CancellationTokenSource
            // This will trigger eviction of all dependent entries
            var dependencyCtsKey = CancellationTokenPrefix + dependencyKey;
            if (_cache.TryGetValue(dependencyCtsKey, out var ctsObj) && ctsObj is CancellationTokenSource cts)
            {
                cts.Cancel();
            }

            // Get all cache keys that depend on this dependency
            if (_reverseIndex.TryGetValue(dependencyKey, out var affectedKeys))
            {
                // Also directly cancel each affected entry's CTS for immediate effect
                foreach (var cacheKey in affectedKeys)
                {
                    if (_cancellationTokens.TryGetValue(cacheKey, out var entryCts))
                    {
                        entryCts.Cancel();
                    }
                }

                // Clean up the reverse index for this dependency
                _reverseIndex.TryRemove(dependencyKey, out _);
            }

            // Clean up dependency CancellationTokenSource after cancellation
            if (_dependencyCancellationTokens.TryRemove(dependencyKey, out var dependencyCts))
            {
                dependencyCts.Dispose();
                // Also remove from cache
                _cache.Remove(dependencyCtsKey);
            }
        }
        finally
        {
            lockObj.Release();
        }
    }

    /// <summary>
    /// Gets or creates a CancellationTokenSource for a dependency key.
    /// Tracks the CTS in both the cache and _dependencyCancellationTokens for proper lifecycle management.
    /// </summary>
    /// <remarks>
    /// The CTS is stored in the cache for dependency tracking and in _dependencyCancellationTokens
    /// for proper disposal. A post-eviction callback ensures cleanup when the cache entry is removed.
    /// </remarks>
    private async Task<CancellationTokenSource?> GetOrCreateDependencyCancellationTokenAsync(
        string dependencyKey,
        CancellationToken cancellationToken)
    {
        var ctsKey = CancellationTokenPrefix + dependencyKey;
        var lockObj = _dependencyLocks.GetOrAdd(dependencyKey, _ => new SemaphoreSlim(1, 1));

        await lockObj.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // Check if CTS already exists in our tracking dictionary
            if (_dependencyCancellationTokens.TryGetValue(dependencyKey, out var existingCts))
            {
                if (!existingCts.IsCancellationRequested)
                {
                    return existingCts;
                }
                else
                {
                    // CTS was cancelled, remove it
                    _dependencyCancellationTokens.TryRemove(dependencyKey, out _);
                    existingCts.Dispose();
                }
            }

            // Create new CTS
            var cts = new CancellationTokenSource();

            // Store in cache with no expiration (managed manually)
            var options = new MemoryCacheEntryOptions
            {
                Priority = CacheItemPriority.NeverRemove
            };

            // Register post-eviction callback to clean up when cache entry is removed
            options.RegisterPostEvictionCallback((key, value, reason, state) =>
            {
                // The 'is' pattern safely handles null - only enters block if value is non-null CancellationTokenSource
                if (value is CancellationTokenSource evictedCts)
                {
                    // Remove from tracking dictionary and dispose
                    if (_dependencyCancellationTokens.TryRemove(dependencyKey, out var trackedCts))
                    {
                        // ReferenceEquals check prevents double-disposal if CTS was replaced
                        if (ReferenceEquals(trackedCts, evictedCts))
                        {
                            trackedCts.Dispose();
                        }
                    }
                }
            });

            _cache.Set(ctsKey, cts, options);

            // Track in our dictionary for proper disposal
            _dependencyCancellationTokens.TryAdd(dependencyKey, cts);

            return cts;
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
    /// Uses semaphore locking for consistency with UpdateReverseIndexAsync.
    /// </summary>
    /// <remarks>
    /// This method is called from cache eviction callbacks, which are synchronous.
    /// We use Wait() instead of WaitAsync() since we're in a synchronous context.
    /// Semaphore usage ensures consistency with other reverse index operations.
    /// A timeout is used to prevent indefinite blocking in case of unexpected issues.
    /// </remarks>
    private void CleanupReverseIndexForKey(string cacheKey)
    {
        // Iterate through all dependencies and remove this cache key
        foreach (var kvp in _reverseIndex.ToArray()) // ToArray to avoid modification during enumeration
        {
            var dependencyKey = kvp.Key;
            var lockObj = _dependencyLocks.GetOrAdd(dependencyKey, _ => new SemaphoreSlim(1, 1));

            // Use synchronous Wait with timeout since this is called from eviction callback
            // Timeout prevents indefinite blocking if semaphore is somehow abandoned
            if (!lockObj.Wait(TimeSpan.FromSeconds(5)))
            {
                // Unable to acquire lock within timeout - skip this dependency
                // This is acceptable as it only affects cleanup, not cache correctness
                continue;
            }

            try
            {
                // Re-check if the dependency still exists after acquiring lock
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
            finally
            {
                lockObj.Release();
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

        // Dispose all dependency CancellationTokenSources
        foreach (var cts in _dependencyCancellationTokens.Values)
        {
            cts?.Dispose();
        }
        _dependencyCancellationTokens.Clear();

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
