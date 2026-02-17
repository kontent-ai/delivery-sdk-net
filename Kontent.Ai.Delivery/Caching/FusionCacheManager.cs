using System.Text.Json;
using Kontent.Ai.Delivery.Logging;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Serialization.SystemTextJson;

namespace Kontent.Ai.Delivery.Caching;

/// <summary>
/// Shared FusionCache-backed implementation of SDK cache manager behavior.
/// </summary>
internal sealed class FusionCacheManager : IDeliveryCacheManager, IDeliveryCachePurger, IDisposable
{
    private readonly IFusionCache _cache;
    private readonly CacheStorageMode _storageMode;
    private readonly TimeSpan _defaultExpiration;
    private readonly Func<string, string> _cacheKeyFormatter;
    private readonly Func<string, string> _dependencyTagFormatter;
    private readonly string _purgeTag;
    private readonly ILogger? _logger;
    private readonly FusionCacheEntryOptions _baseWriteOptions;
    private readonly FusionCacheEntryOptions _baseReadOptions;
    private readonly FusionCacheEntryOptions _baseInvalidateOptions;
    private readonly bool _ownsFusionCache;
    private int _disposeState;

    private FusionCacheManager(
        IFusionCache cache,
        CacheStorageMode storageMode,
        TimeSpan defaultExpiration,
        Func<string, string> cacheKeyFormatter,
        Func<string, string> dependencyTagFormatter,
        string purgeTag,
        ILogger? logger,
        FusionCacheEntryOptions baseReadOptions,
        FusionCacheEntryOptions baseWriteOptions,
        FusionCacheEntryOptions baseInvalidateOptions,
        bool ownsFusionCache)
    {
        _cache = cache;
        _storageMode = storageMode;
        _defaultExpiration = defaultExpiration;
        _cacheKeyFormatter = cacheKeyFormatter;
        _dependencyTagFormatter = dependencyTagFormatter;
        _purgeTag = purgeTag;
        _logger = logger;
        _baseReadOptions = baseReadOptions;
        _baseWriteOptions = baseWriteOptions;
        _baseInvalidateOptions = baseInvalidateOptions;
        _ownsFusionCache = ownsFusionCache;
    }

    public static FusionCacheManager CreateMemory(
        IMemoryCache memoryCache,
        string? keyPrefix = null,
        TimeSpan? defaultExpiration = null,
        ILogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(memoryCache);

        var effectiveExpiration = defaultExpiration ?? TimeSpan.FromHours(1);
        var prefixSegment = string.IsNullOrEmpty(keyPrefix) ? "" : $"{keyPrefix}:";

        var fusion = new FusionCache(
            Options.Create(new FusionCacheOptions
            {
                CacheName = $"KontentDelivery.Memory.{(string.IsNullOrWhiteSpace(keyPrefix) ? "Default" : keyPrefix)}",
                DistributedCacheKeyModifierMode = CacheKeyModifierMode.None,
                DefaultEntryOptions = new FusionCacheEntryOptions
                {
                    Duration = effectiveExpiration,
                    IsFailSafeEnabled = false,
                    AllowBackgroundDistributedCacheOperations = false,
                    AllowBackgroundBackplaneOperations = false,
                    ReThrowDistributedCacheExceptions = false,
                    ReThrowSerializationExceptions = true,
                    ReThrowBackplaneExceptions = false
                }
            }),
            memoryCache,
            logger: null);

        return new FusionCacheManager(
            fusion,
            CacheStorageMode.HydratedObject,
            effectiveExpiration,
            cacheKey => $"{prefixSegment}{cacheKey}",
            dependency => $"{prefixSegment}{dependency}",
            purgeTag: $"{prefixSegment}__purge_all",
            logger,
            baseReadOptions: new FusionCacheEntryOptions
            {
                SkipMemoryCacheRead = false,
                SkipMemoryCacheWrite = true,
                SkipDistributedCacheRead = true,
                SkipDistributedCacheWrite = true,
                ReThrowDistributedCacheExceptions = false,
                ReThrowSerializationExceptions = false,
                ReThrowBackplaneExceptions = false,
                AllowBackgroundBackplaneOperations = false,
                AllowBackgroundDistributedCacheOperations = false
            },
            baseWriteOptions: new FusionCacheEntryOptions
            {
                Duration = effectiveExpiration,
                IsFailSafeEnabled = false,
                SkipMemoryCacheRead = true,
                SkipMemoryCacheWrite = false,
                SkipDistributedCacheRead = true,
                SkipDistributedCacheWrite = true,
                ReThrowDistributedCacheExceptions = false,
                ReThrowSerializationExceptions = true,
                ReThrowBackplaneExceptions = false,
                AllowBackgroundBackplaneOperations = false,
                AllowBackgroundDistributedCacheOperations = false
            },
            baseInvalidateOptions: new FusionCacheEntryOptions
            {
                SkipMemoryCacheRead = false,
                SkipMemoryCacheWrite = false,
                SkipDistributedCacheRead = true,
                SkipDistributedCacheWrite = true,
                ReThrowDistributedCacheExceptions = false,
                ReThrowSerializationExceptions = false,
                ReThrowBackplaneExceptions = false,
                AllowBackgroundBackplaneOperations = false,
                AllowBackgroundDistributedCacheOperations = false
            },
            ownsFusionCache: true);
    }

    public static FusionCacheManager CreateDistributed(
        IDistributedCache distributedCache,
        string? keyPrefix = null,
        TimeSpan? defaultExpiration = null,
        JsonSerializerOptions? serializerOptions = null,
        ILogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(distributedCache);

        var effectiveExpiration = defaultExpiration ?? TimeSpan.FromHours(1);
        var prefixSegment = string.IsNullOrEmpty(keyPrefix) ? "" : $"{keyPrefix}:";
        var fusion = new FusionCache(
            Options.Create(new FusionCacheOptions
            {
                CacheName = $"KontentDelivery.Distributed.{(string.IsNullOrWhiteSpace(keyPrefix) ? "Default" : keyPrefix)}",
                DistributedCacheKeyModifierMode = CacheKeyModifierMode.None,
                DefaultEntryOptions = new FusionCacheEntryOptions
                {
                    Duration = effectiveExpiration,
                    IsFailSafeEnabled = false,
                    AllowBackgroundDistributedCacheOperations = false,
                    AllowBackgroundBackplaneOperations = false,
                    ReThrowDistributedCacheExceptions = false,
                    ReThrowSerializationExceptions = true,
                    ReThrowBackplaneExceptions = false,
                    SkipMemoryCacheRead = true,
                    SkipMemoryCacheWrite = true
                }
            }),
            memoryCache: null,
            logger: null);

        var serializer = new FusionCacheSystemTextJsonSerializer(serializerOptions);
        fusion.SetupDistributedCache(distributedCache, serializer);

        return new FusionCacheManager(
            fusion,
            CacheStorageMode.RawJson,
            effectiveExpiration,
            cacheKey => $"{prefixSegment}cache:{cacheKey}",
            dependency => $"{prefixSegment}dep:{dependency}",
            purgeTag: $"{prefixSegment}dep:__purge_all",
            logger,
            baseReadOptions: new FusionCacheEntryOptions
            {
                SkipMemoryCacheRead = false,
                SkipMemoryCacheWrite = false,
                SkipDistributedCacheRead = false,
                SkipDistributedCacheWrite = true,
                ReThrowDistributedCacheExceptions = false,
                ReThrowSerializationExceptions = false,
                ReThrowBackplaneExceptions = false,
                AllowBackgroundBackplaneOperations = false,
                AllowBackgroundDistributedCacheOperations = false
            },
            baseWriteOptions: new FusionCacheEntryOptions
            {
                Duration = effectiveExpiration,
                IsFailSafeEnabled = false,
                SkipMemoryCacheRead = false,
                SkipMemoryCacheWrite = false,
                SkipDistributedCacheRead = true,
                SkipDistributedCacheWrite = false,
                ReThrowDistributedCacheExceptions = true,
                ReThrowSerializationExceptions = true,
                ReThrowBackplaneExceptions = false,
                AllowBackgroundBackplaneOperations = false,
                AllowBackgroundDistributedCacheOperations = false
            },
            baseInvalidateOptions: new FusionCacheEntryOptions
            {
                SkipMemoryCacheRead = false,
                SkipMemoryCacheWrite = false,
                SkipDistributedCacheRead = false,
                SkipDistributedCacheWrite = false,
                ReThrowDistributedCacheExceptions = false,
                ReThrowSerializationExceptions = false,
                ReThrowBackplaneExceptions = false,
                AllowBackgroundBackplaneOperations = false,
                AllowBackgroundDistributedCacheOperations = false
            },
            ownsFusionCache: true);
    }

    public CacheStorageMode StorageMode => _storageMode;

    public async Task<T?> GetAsync<T>(string cacheKey, CancellationToken cancellationToken = default)
        where T : class
    {
        ThrowIfDisposed();

        if (string.IsNullOrWhiteSpace(cacheKey))
        {
            return null;
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var maybe = await _cache.TryGetAsync<T>(
                    _cacheKeyFormatter(cacheKey),
                    _baseReadOptions,
                    cancellationToken)
                .ConfigureAwait(false);

            return maybe.HasValue ? maybe.Value : null;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            if (_logger is not null)
            {
                LoggerMessages.CacheGetFailed(_logger, cacheKey, ex);
            }

            return null;
        }
    }

    public async Task SetAsync<T>(
        string cacheKey,
        T value,
        IEnumerable<string> dependencies,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
        where T : class
    {
        ThrowIfDisposed();

        ArgumentNullException.ThrowIfNull(cacheKey);
        if (string.IsNullOrWhiteSpace(cacheKey))
            throw new ArgumentException("Cache key cannot be empty or whitespace.", nameof(cacheKey));
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(dependencies);

        cancellationToken.ThrowIfCancellationRequested();

        // Materialize first so dependency enumeration errors keep Set semantics deterministic.
        var dependencyTags = dependencies
            .Where(d => !string.IsNullOrWhiteSpace(d))
            .Select(_dependencyTagFormatter)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var tags = dependencyTags.Length == 0
            ? new[] { _purgeTag }
            : dependencyTags.Concat(new[] { _purgeTag }).ToArray();

        var writeOptions = _baseWriteOptions.Duplicate(
            expiration ?? _defaultExpiration);

        try
        {
            await _cache.SetAsync(
                    _cacheKeyFormatter(cacheKey),
                    value,
                    writeOptions,
                    tags,
                    cancellationToken)
                .ConfigureAwait(false);

            if (_logger is not null)
                LoggerMessages.CacheSetCompleted(_logger, cacheKey, dependencyTags.Length);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            if (_logger is not null)
            {
                LoggerMessages.CacheSetFailed(_logger, cacheKey, ex);
            }

            throw;
        }
    }

    public async Task InvalidateAsync(CancellationToken cancellationToken = default, params string[] dependencyKeys)
    {
        ThrowIfDisposed();

        if (dependencyKeys is null || dependencyKeys.Length == 0)
        {
            return;
        }

        var validKeys = dependencyKeys.Where(k => !string.IsNullOrWhiteSpace(k)).ToArray();

        if (_logger is not null && validKeys.Length > 0)
            LoggerMessages.CacheInvalidateStarting(_logger, validKeys.Length);

        try
        {
            await _cache.RemoveByTagAsync(
                    validKeys.Select(_dependencyTagFormatter),
                    _baseInvalidateOptions,
                    cancellationToken)
                .ConfigureAwait(false);

            if (_logger is not null)
            {
                foreach (var dependencyKey in validKeys)
                {
                    LoggerMessages.CacheInvalidateCompleted(_logger, dependencyKey);
                }
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            if (_logger is not null)
                LoggerMessages.CacheInvalidationFailed(_logger, ex);
        }
    }

    public async Task PurgeAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();

        await _cache.RemoveByTagAsync(
                _purgeTag,
                _baseInvalidateOptions,
                cancellationToken)
            .ConfigureAwait(false);
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposeState, 1) != 0)
            return;

        if (_ownsFusionCache)
        {
            _cache.Dispose();
        }
    }

    private void ThrowIfDisposed() =>
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposeState) != 0, nameof(FusionCacheManager));
}
