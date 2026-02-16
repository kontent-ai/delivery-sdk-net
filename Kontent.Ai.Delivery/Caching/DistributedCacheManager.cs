using System.Text;
using System.Text.Json;
using Kontent.Ai.Delivery.Logging;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

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
/// <item><description>Reverse index entries map "dep:{dependencyKey}" to a payload containing cache keys and max expiration metadata</description></item>
/// </list>
/// </para>
/// <para>
/// The SDK caches raw JSON strings (via <see cref="CachedRawItemsPayload"/>) rather than
/// hydrated C# objects.
/// This approach avoids serialization issues with complex object graphs (circular references,
/// custom converters, etc.) and simplifies the serialization configuration.
/// </para>
/// <para>
/// Reverse index entries track the maximum known expiration across associated cache entries.
/// This prevents index TTL shrink when shorter-lived entries are added later for the same dependency.
/// </para>
/// <para>
/// <b>IMPORTANT: Race Conditions &amp; Eventual Consistency</b>
/// <br/>
/// This implementation relies on a "read-modify-write" pattern for maintaining the dependency reverse index,
/// without using distributed locking (e.g., RedLock).
/// </para>
/// <para>
/// <b>Risk:</b> During high concurrency, two processes updating the same dependency key simultaneously
/// may overwrite each other's updates. This results in "lost updates" to the reverse index.
/// </para>
/// <para>
/// <b>Consequence:</b> A cache entry might become "orphaned" from its dependency. If that dependency is later invalidated
/// (e.g., via webhook), the orphaned entry will <b>NOT</b> be invalidated and will remain stale until its
/// natural expiration (TTL) is reached.
/// </para>
/// <para>
/// <b>Mitigation:</b>
/// <list type="bullet">
/// <item><description>Keep cache TTLs reasonable (e.g., &lt; 1 hour) to bound the maximum staleness.</description></item>
/// <item><description>If strict consistency is required, implement a custom <see cref="IDeliveryCacheManager"/> with atomic operations or distributed locks.</description></item>
/// </list>
/// </para>
/// </remarks>
/// <remarks>
/// Initializes a new instance of the <see cref="DistributedCacheManager"/> class with custom JSON serialization options.
/// </remarks>
/// <param name="cache">The distributed cache implementation (Redis, SQL Server, etc.).</param>
/// <param name="keyPrefix">
/// Optional prefix for all cache keys. Used to isolate cache entries when multiple clients share the same distributed cache.
/// For example, "production" or "preview:myproject".
/// </param>
/// <param name="defaultExpiration">
/// Default expiration time for cache entries. If null, defaults to 1 hour.
/// </param>
/// <param name="jsonSerializerOptions">
/// Custom JSON serialization options. If null, simple default options are used.
/// The SDK caches raw JSON strings, so complex reference handling is not needed.
/// </param>
/// <param name="logger">Optional logger for cache operations.</param>
/// <exception cref="ArgumentNullException">
/// Thrown when <paramref name="cache"/> is null.
/// </exception>
internal sealed class DistributedCacheManager(
    IDistributedCache cache,
    string? keyPrefix,
    TimeSpan? defaultExpiration,
    JsonSerializerOptions? jsonSerializerOptions,
    ILogger<DistributedCacheManager>? logger = null) : IDeliveryCacheManager
{
    private sealed class DependencyIndexPayload
    {
        public HashSet<string> CacheKeys { get; init; } = new(StringComparer.OrdinalIgnoreCase);
        public DateTimeOffset MaxAbsoluteExpirationUtc { get; init; }
    }

    private readonly TimeSpan _defaultExpiration = defaultExpiration ?? TimeSpan.FromHours(1);
    private readonly JsonSerializerOptions _jsonOptions = jsonSerializerOptions ?? DefaultJsonSerializerOptions;

    private static readonly JsonSerializerOptions DefaultJsonSerializerOptions = new()
    {
        WriteIndented = false,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    private const string CacheKeyPrefix = "cache:";
    private const string DependencyKeyPrefix = "dep:";
    private readonly string _keyPrefixSegment = string.IsNullOrEmpty(keyPrefix) ? "" : $"{keyPrefix}:";

    /// <inheritdoc />
    public CacheStorageMode StorageMode => CacheStorageMode.RawJson;

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
    /// <param name="logger">Optional logger for cache operations.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="cache"/> is null.
    /// </exception>
    public DistributedCacheManager(
        IDistributedCache cache,
        string? keyPrefix = null,
        TimeSpan? defaultExpiration = null,
        ILogger<DistributedCacheManager>? logger = null)
        : this(cache, keyPrefix, defaultExpiration, jsonSerializerOptions: null, logger)
    {
    }

    /// <inheritdoc />
    public async Task<T?> GetAsync<T>(string cacheKey, CancellationToken cancellationToken = default)
        where T : class
    {
        if (string.IsNullOrWhiteSpace(cacheKey))
            return null;

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var prefixedKey = GetCacheKey(cacheKey);
            var bytes = await cache.GetAsync(prefixedKey, cancellationToken).ConfigureAwait(false);

            if (bytes is null || bytes.Length == 0)
                return null;

            var json = Encoding.UTF8.GetString(bytes);
            var value = JsonSerializer.Deserialize<T>(json, _jsonOptions);

            return value;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            if (logger is not null)
            {
                LoggerMessages.CacheDeserializationFailed(logger, cacheKey, typeof(T).Name, ex);
            }

            // Treat any other exception as a cache miss
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
        ArgumentNullException.ThrowIfNull(cacheKey);
        if (string.IsNullOrWhiteSpace(cacheKey))
            throw new ArgumentException("Cache key cannot be empty or whitespace.", nameof(cacheKey));
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(dependencies);

        cancellationToken.ThrowIfCancellationRequested();

        var json = SerializeWithValidation(value);
        var bytes = Encoding.UTF8.GetBytes(json);
        var dependencyList = dependencies.Where(d => !string.IsNullOrWhiteSpace(d)).ToList();
        var entryAbsoluteExpiration = DateTimeOffset.UtcNow.Add(expiration ?? _defaultExpiration);

        var cacheOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpiration = entryAbsoluteExpiration
        };

        var prefixedKey = GetCacheKey(cacheKey);

        await cache.SetAsync(prefixedKey, bytes, cacheOptions, cancellationToken)
            .ConfigureAwait(false);

        foreach (var dependency in dependencyList)
        {
            try
            {
                await AddCacheKeyToDependencyIndexAsync(
                    dependency,
                    cacheKey,
                    entryAbsoluteExpiration,
                    cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (logger is not null)
                    LoggerMessages.CacheSetFailed(logger, $"dep:{dependency}", ex);
            }
        }

        if (logger is not null)
            LoggerMessages.CacheSetCompleted(logger, cacheKey, dependencyList.Count);
    }

    /// <inheritdoc />
    public async Task InvalidateAsync(CancellationToken cancellationToken = default, params string[] dependencyKeys)
    {
        if (dependencyKeys is null || dependencyKeys.Length == 0)
        {
            return;
        }

        var validKeys = dependencyKeys.Where(k => !string.IsNullOrWhiteSpace(k)).ToList();

        if (logger is not null && validKeys.Count > 0)
            LoggerMessages.CacheInvalidateStarting(logger, validKeys.Count);

        try
        {
            var tasks = validKeys.Select(k => InvalidateDependencyAsync(k, cancellationToken));

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            if (logger is not null)
                LoggerMessages.CacheInvalidationFailed(logger, ex);
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

            return string.IsNullOrWhiteSpace(json)
                ? throw new InvalidOperationException(
                    $"Serialization of type {typeof(T).Name} produced null or empty JSON. " +
                    $"The type may not be serializable for distributed caching.")
                : json;
        }
        catch (NotSupportedException ex)
        {
            if (logger is not null)
                LoggerMessages.CacheSerializationFailed(logger, "unknown", typeof(T).Name, ex);

            throw new InvalidOperationException(
                $"Type {typeof(T).Name} cannot be serialized for distributed caching. " +
                $"Consider using MemoryCacheManager instead or implementing custom serialization. " +
                $"Error: {ex.Message}",
                ex);
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            if (logger is not null)
                LoggerMessages.CacheSerializationFailed(logger, "unknown", typeof(T).Name, ex);

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
            var indexBytes = await cache.GetAsync(indexKey, cancellationToken).ConfigureAwait(false);

            if (indexBytes is null || indexBytes.Length == 0)
            {
                return;
            }

            var indexJson = Encoding.UTF8.GetString(indexBytes);
            var cacheKeys = DeserializeDependencyIndexKeys(indexJson);

            if (cacheKeys is null || cacheKeys.Count == 0)
            {
                await cache.RemoveAsync(indexKey, cancellationToken).ConfigureAwait(false);
                return;
            }

            var removalTasks = cacheKeys.Select(key =>
                cache.RemoveAsync(GetCacheKey(key), cancellationToken));

            await Task.WhenAll(removalTasks).ConfigureAwait(false);

            await cache.RemoveAsync(indexKey, cancellationToken).ConfigureAwait(false);

            if (logger is not null)
                LoggerMessages.CacheInvalidateCompleted(logger, dependencyKey);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            if (logger is not null)
                LoggerMessages.CacheBestEffortFailed(logger, $"invalidate:{dependencyKey}", ex);
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
        DateTimeOffset entryAbsoluteExpiration,
        CancellationToken cancellationToken)
    {
        var indexKey = GetDependencyKey(dependencyKey);

        try
        {
            var existingBytes = await cache.GetAsync(indexKey, cancellationToken).ConfigureAwait(false);
            var cacheKeySet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var maxAbsoluteExpiration = entryAbsoluteExpiration;

            if (existingBytes is not null && existingBytes.Length > 0)
            {
                var existingJson = Encoding.UTF8.GetString(existingBytes);
                if (TryDeserializeDependencyIndexPayload(existingJson, _jsonOptions, out var existingPayload))
                {
                    cacheKeySet = new HashSet<string>(existingPayload.CacheKeys, StringComparer.OrdinalIgnoreCase);
                    if (existingPayload.MaxAbsoluteExpirationUtc > maxAbsoluteExpiration)
                    {
                        maxAbsoluteExpiration = existingPayload.MaxAbsoluteExpirationUtc;
                    }
                }
            }

            cacheKeySet.Add(cacheKey);

            var indexPayload = new DependencyIndexPayload
            {
                CacheKeys = cacheKeySet,
                MaxAbsoluteExpirationUtc = maxAbsoluteExpiration
            };

            var indexJson = JsonSerializer.Serialize(indexPayload, _jsonOptions);
            var indexBytes = Encoding.UTF8.GetBytes(indexJson);
            var indexOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = maxAbsoluteExpiration
            };

            await cache.SetAsync(indexKey, indexBytes, indexOptions, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            if (logger is not null)
                LoggerMessages.CacheBestEffortFailed(logger, $"index:{dependencyKey}", ex);
        }
    }

    private HashSet<string>? DeserializeDependencyIndexKeys(string indexJson)
        => TryDeserializeDependencyIndexPayload(indexJson, _jsonOptions, out var payload)
            ? new HashSet<string>(payload.CacheKeys, StringComparer.OrdinalIgnoreCase)
            : null;

    private static bool TryDeserializeDependencyIndexPayload(
        string indexJson,
        JsonSerializerOptions jsonOptions,
        out DependencyIndexPayload payload)
    {
        try
        {
            var parsed = JsonSerializer.Deserialize<DependencyIndexPayload>(indexJson, jsonOptions);
            if (parsed?.CacheKeys is null)
            {
                payload = null!;
                return false;
            }

            payload = parsed;
            return true;
        }
        catch (JsonException)
        {
            payload = null!;
            return false;
        }
        catch (NotSupportedException)
        {
            payload = null!;
            return false;
        }
    }

    private string GetCacheKey(string cacheKey) =>
        $"{_keyPrefixSegment}{CacheKeyPrefix}{cacheKey}";

    private string GetDependencyKey(string dependencyKey) =>
        $"{_keyPrefixSegment}{DependencyKeyPrefix}{dependencyKey}";
}
