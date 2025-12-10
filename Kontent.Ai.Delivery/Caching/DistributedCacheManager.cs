using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace Kontent.Ai.Delivery.Caching;

/// <summary>
/// Distributed cache implementation of <see cref="IDeliveryCacheManager"/> using IDistributedCache.
/// Provides thread-safe caching with automatic dependency-based invalidation across multiple application instances.
/// </summary>
/// <remarks>
/// <para>
/// This implementation uses a reverse index pattern to enable efficient cache invalidation:
/// <list type="bullet">
/// <item><description>Cache entries are stored with prefix "cache:"</description></item>
/// <item><description>Reverse index entries map "dep:{dependencyKey}" to HashSet of cache keys</description></item>
/// </list>
/// </para>
/// <para>
/// Serialization uses System.Text.Json with ReferenceHandler.Preserve to handle circular references.
/// The JsonSerializerOptions are configured to handle deep object graphs and null values gracefully.
/// Serialization failures throw InvalidOperationException with helpful error messages.
/// </para>
/// <para>
/// The reverse index entries share the same expiration as their associated cache entries,
/// preventing orphaned index entries while maintaining consistency.
/// </para>
/// <para>
/// <b>Thread Safety and Eventual Consistency:</b><br/>
/// The reverse index uses a read-modify-write pattern without distributed locking.
/// This creates a race condition where concurrent updates to the same dependency key may result
/// in lost index entries (eventual consistency).
/// </para>
/// <para>
/// <b>Race Condition Example:</b><br/>
/// If two cache entries with the same dependency are stored concurrently:<br/>
/// 1. Thread 1 reads index: ["key1"]<br/>
/// 2. Thread 2 reads index: ["key1"] (same)<br/>
/// 3. Thread 1 writes: ["key1", "key2"]<br/>
/// 4. Thread 2 writes: ["key1", "key3"] (overwrites, losing "key2")<br/>
/// Result: key2's dependency association is lost.
/// </para>
/// <para>
/// <b>Why This Is Acceptable:</b>
/// <list type="bullet">
/// <item><description>Worst case: Some cache entries survive invalidation and expire naturally via TTL</description></item>
/// <item><description>Application correctness doesn't depend on perfect invalidation (only freshness)</description></item>
/// <item><description>The probability decreases as cache expiration times are typically much longer than write bursts</description></item>
/// <item><description>Simplicity and provider neutrality outweigh the small risk of stale data</description></item>
/// </list>
/// </para>
/// <para>
/// For strict consistency requirements, consider implementing distributed locking (e.g., RedLock for Redis)
/// or using a cache provider that supports atomic SET operations.
/// </para>
/// </remarks>
public sealed class DistributedCacheManager : IDeliveryCacheManager
{
    private readonly IDistributedCache _cache;
    private readonly string? _keyPrefix;
    private readonly TimeSpan _defaultExpiration;
    private readonly JsonSerializerOptions _jsonOptions;

    private const string CacheKeyPrefix = "cache:";
    private const string DependencyKeyPrefix = "dep:";

    private string KeyPrefixSegment => _keyPrefix is null ? "" : $"{_keyPrefix}:";

    /// <summary>
    /// Initializes a new instance of the <see cref="DistributedCacheManager"/> class.
    /// </summary>
    /// <param name="cache">The distributed cache implementation (Redis, SQL Server, etc.).</param>
    /// <param name="keyPrefix">
    /// Optional prefix for all cache keys. Used to isolate cache entries when multiple clients share the same distributed cache.
    /// For example, "production" or "preview:myproject".
    /// </param>
    /// <param name="defaultExpiration">
    /// Default expiration time for cache entries. If null, defaults to 1 hour.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="cache"/> is null.
    /// </exception>
    public DistributedCacheManager(
        IDistributedCache cache,
        string? keyPrefix = null,
        TimeSpan? defaultExpiration = null)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _keyPrefix = keyPrefix;
        _defaultExpiration = defaultExpiration ?? TimeSpan.FromHours(1);

        _jsonOptions = new JsonSerializerOptions
        {
            ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve,
            WriteIndented = false,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
            MaxDepth = 64 // Handle deep object graphs (e.g., nested content items)
        };
    }

    /// <inheritdoc />
    public async Task<T?> GetAsync<T>(string cacheKey, CancellationToken cancellationToken = default)
        where T : class
    {
        if (string.IsNullOrWhiteSpace(cacheKey))
        {
            // Return null for invalid keys rather than throwing
            // This aligns with the interface contract for cache misses
            return null;
        }

        try
        {
            // Check cancellation before cache operation
            cancellationToken.ThrowIfCancellationRequested();

            var prefixedKey = GetCacheKey(cacheKey);
            var bytes = await _cache.GetAsync(prefixedKey, cancellationToken).ConfigureAwait(false);

            if (bytes == null || bytes.Length == 0)
            {
                return null;
            }

            // Deserialize from JSON
            var json = Encoding.UTF8.GetString(bytes);
            var value = JsonSerializer.Deserialize<T>(json, _jsonOptions);

            return value;
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
            return null;
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

        // Serialize and validate - let serialization exceptions propagate per interface contract
        var json = SerializeWithValidation(value);
        var bytes = Encoding.UTF8.GetBytes(json);

        // Create cache entry options
        var cacheOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration ?? _defaultExpiration
        };

        var prefixedKey = GetCacheKey(cacheKey);

        // Store in cache - let exceptions propagate per interface contract
        await _cache.SetAsync(prefixedKey, bytes, cacheOptions, cancellationToken)
            .ConfigureAwait(false);

        // Update reverse index for each dependency (best effort - failures here are acceptable)
        var dependencyList = dependencies.Where(d => !string.IsNullOrWhiteSpace(d)).ToList();

        foreach (var dependency in dependencyList)
        {
            try
            {
                await AddCacheKeyToDependencyIndexAsync(
                    dependency,
                    cacheKey,
                    cacheOptions,
                    cancellationToken)
                    .ConfigureAwait(false);
            }
            catch
            {
                // Reverse index update failure should not break caching
                // Worst case: invalidation might miss this entry
            }
        }
    }

    /// <inheritdoc />
    public async Task InvalidateAsync(CancellationToken cancellationToken = default, params string[] dependencyKeys)
    {
        if (dependencyKeys == null || dependencyKeys.Length == 0)
        {
            // No dependencies to invalidate - this is valid (idempotent)
            return;
        }

        try
        {
            // Process each dependency key
            var tasks = dependencyKeys
                .Where(k => !string.IsNullOrWhiteSpace(k))
                .Select(k => InvalidateDependencyAsync(k, cancellationToken));

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Re-throw cancellation exceptions
            throw;
        }
        catch
        {
            // Invalidation failures should not break the application
            // Worst case: stale entries remain until TTL expires
        }
    }

    /// <summary>
    /// Serializes a value to JSON with validation.
    /// </summary>
    /// <typeparam name="T">The type to serialize.</typeparam>
    /// <param name="value">The value to serialize.</param>
    /// <returns>The serialized JSON string.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when serialization fails or produces invalid output.
    /// </exception>
    private string SerializeWithValidation<T>(T value) where T : class
    {
        try
        {
            var json = JsonSerializer.Serialize(value, _jsonOptions);

            if (string.IsNullOrWhiteSpace(json))
            {
                throw new InvalidOperationException(
                    $"Serialization of type {typeof(T).Name} produced null or empty JSON. " +
                    $"The type may not be serializable for distributed caching.");
            }

            return json;
        }
        catch (NotSupportedException ex)
        {
            throw new InvalidOperationException(
                $"Type {typeof(T).Name} cannot be serialized for distributed caching. " +
                $"Consider using MemoryCacheManager instead or implementing custom serialization. " +
                $"Error: {ex.Message}",
                ex);
        }
        catch (InvalidOperationException)
        {
            // Re-throw our own exceptions
            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to serialize type {typeof(T).Name} for distributed caching. " +
                $"The type may contain unsupported constructs (e.g., delegates, circular references without proper handling). " +
                $"Error: {ex.Message}",
                ex);
        }
    }

    /// <summary>
    /// Invalidates all cache entries that depend on the specified dependency key.
    /// </summary>
    private async Task InvalidateDependencyAsync(string dependencyKey, CancellationToken cancellationToken)
    {
        var indexKey = GetDependencyKey(dependencyKey);

        try
        {
            // Get the reverse index
            var indexBytes = await _cache.GetAsync(indexKey, cancellationToken).ConfigureAwait(false);

            if (indexBytes == null || indexBytes.Length == 0)
            {
                return;
            }

            // Deserialize the set of cache keys
            var indexJson = Encoding.UTF8.GetString(indexBytes);
            var cacheKeys = JsonSerializer.Deserialize<HashSet<string>>(indexJson, _jsonOptions);

            if (cacheKeys == null || cacheKeys.Count == 0)
            {
                // Remove corrupted index
                await _cache.RemoveAsync(indexKey, cancellationToken).ConfigureAwait(false);
                return;
            }

            // Remove all associated cache entries
            var removalTasks = cacheKeys.Select(key =>
                _cache.RemoveAsync(GetCacheKey(key), cancellationToken));

            await Task.WhenAll(removalTasks).ConfigureAwait(false);

            // Remove the dependency index itself
            await _cache.RemoveAsync(indexKey, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            // Continue with other invalidations even if this one fails
        }
    }

    /// <summary>
    /// Adds a cache key to the reverse index for a given dependency.
    /// Uses read-modify-write pattern to maintain the set of dependent cache keys.
    /// </summary>
    /// <remarks>
    /// This method has a race condition (see class-level remarks), but failures are acceptable
    /// since they only affect cache invalidation efficiency, not application correctness.
    /// </remarks>
    private async Task AddCacheKeyToDependencyIndexAsync(
        string dependencyKey,
        string cacheKey,
        DistributedCacheEntryOptions cacheOptions,
        CancellationToken cancellationToken)
    {
        var indexKey = GetDependencyKey(dependencyKey);

        try
        {
            // Read existing index
            var existingBytes = await _cache.GetAsync(indexKey, cancellationToken).ConfigureAwait(false);
            HashSet<string> cacheKeySet;

            if (existingBytes != null && existingBytes.Length > 0)
            {
                var existingJson = Encoding.UTF8.GetString(existingBytes);
                cacheKeySet = JsonSerializer.Deserialize<HashSet<string>>(existingJson, _jsonOptions)
                    ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }
            else
            {
                cacheKeySet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }

            // Add new cache key
            cacheKeySet.Add(cacheKey);

            // Serialize and write back
            var indexJson = JsonSerializer.Serialize(cacheKeySet, _jsonOptions);
            var indexBytes = Encoding.UTF8.GetBytes(indexJson);

            await _cache.SetAsync(indexKey, indexBytes, cacheOptions, cancellationToken)
                .ConfigureAwait(false);
        }
        catch
        {
            // Reverse index update failure should not break caching
            // Worst case: invalidation might miss this entry
        }
    }

    private string GetCacheKey(string cacheKey) =>
        $"{KeyPrefixSegment}{CacheKeyPrefix}{cacheKey}";

    private string GetDependencyKey(string dependencyKey) =>
        $"{KeyPrefixSegment}{DependencyKeyPrefix}{dependencyKey}";
}
