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
    private readonly ILogger? _logger;
    private readonly FusionCacheEntryOptions _baseWriteOptions;
    private readonly FusionCacheEntryOptions _baseInvalidateOptions;
    private readonly bool _ownsFusionCache;
    private int _disposeState;

    private FusionCacheManager(
        IFusionCache cache,
        CacheStorageMode storageMode,
        TimeSpan defaultExpiration,
        Func<string, string> cacheKeyFormatter,
        Func<string, string> dependencyTagFormatter,
        ILogger? logger,
        FusionCacheEntryOptions baseWriteOptions,
        FusionCacheEntryOptions baseInvalidateOptions,
        bool ownsFusionCache)
    {
        _cache = cache;
        _storageMode = storageMode;
        _defaultExpiration = defaultExpiration;
        _cacheKeyFormatter = cacheKeyFormatter;
        _dependencyTagFormatter = dependencyTagFormatter;
        _logger = logger;
        _baseWriteOptions = baseWriteOptions;
        _baseInvalidateOptions = baseInvalidateOptions;
        _ownsFusionCache = ownsFusionCache;
    }

    public static FusionCacheManager CreateMemory(
        IMemoryCache memoryCache,
        DeliveryCacheOptions cacheOptions,
        ILogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(memoryCache);
        ArgumentNullException.ThrowIfNull(cacheOptions);

        var effectiveExpiration = cacheOptions.DefaultExpiration;
        var keyPrefix = cacheOptions.KeyPrefix;
        var prefixSegment = string.IsNullOrEmpty(keyPrefix) ? "" : $"{keyPrefix}:";

        var defaultEntryOptions = new FusionCacheEntryOptions
        {
            Duration = effectiveExpiration,
            IsFailSafeEnabled = cacheOptions.IsFailSafeEnabled,
            FailSafeMaxDuration = cacheOptions.FailSafeMaxDuration,
            FailSafeThrottleDuration = cacheOptions.FailSafeThrottleDuration,
            JitterMaxDuration = cacheOptions.JitterMaxDuration,
            AllowBackgroundDistributedCacheOperations = false,
            AllowBackgroundBackplaneOperations = false,
            ReThrowDistributedCacheExceptions = false,
            ReThrowSerializationExceptions = true,
            ReThrowBackplaneExceptions = false
        };

        if (cacheOptions.EagerRefreshThreshold > 0)
        {
            defaultEntryOptions.EagerRefreshThreshold = cacheOptions.EagerRefreshThreshold;
        }

        var fusion = new FusionCache(
            Options.Create(new FusionCacheOptions
            {
                CacheName = $"KontentDelivery.Memory.{(string.IsNullOrWhiteSpace(keyPrefix) ? "Default" : keyPrefix)}",
                DistributedCacheKeyModifierMode = CacheKeyModifierMode.None,
                DefaultEntryOptions = defaultEntryOptions
            }),
            memoryCache,
            logger: null);

        var baseWriteOptions = new FusionCacheEntryOptions
        {
            Duration = effectiveExpiration,
            IsFailSafeEnabled = cacheOptions.IsFailSafeEnabled,
            FailSafeMaxDuration = cacheOptions.FailSafeMaxDuration,
            FailSafeThrottleDuration = cacheOptions.FailSafeThrottleDuration,
            JitterMaxDuration = cacheOptions.JitterMaxDuration,
            SkipMemoryCacheRead = false,
            SkipMemoryCacheWrite = false,
            SkipDistributedCacheRead = true,
            SkipDistributedCacheWrite = true,
            ReThrowDistributedCacheExceptions = false,
            ReThrowSerializationExceptions = true,
            ReThrowBackplaneExceptions = false,
            AllowBackgroundBackplaneOperations = false,
            AllowBackgroundDistributedCacheOperations = false
        };

        if (cacheOptions.EagerRefreshThreshold > 0)
        {
            baseWriteOptions.EagerRefreshThreshold = cacheOptions.EagerRefreshThreshold;
        }

        return new FusionCacheManager(
            fusion,
            CacheStorageMode.HydratedObject,
            effectiveExpiration,
            cacheKey => $"{prefixSegment}{cacheKey}",
            dependency => $"{prefixSegment}{dependency}",
            logger,
            baseWriteOptions: baseWriteOptions,
            baseInvalidateOptions: new FusionCacheEntryOptions
            {
                IsFailSafeEnabled = false,
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
        DeliveryCacheOptions cacheOptions,
        JsonSerializerOptions? serializerOptions = null,
        ILogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(distributedCache);
        ArgumentNullException.ThrowIfNull(cacheOptions);

        var effectiveExpiration = cacheOptions.DefaultExpiration;
        var keyPrefix = cacheOptions.KeyPrefix;
        var prefixSegment = string.IsNullOrEmpty(keyPrefix) ? "" : $"{keyPrefix}:";

        var defaultEntryOptions = new FusionCacheEntryOptions
        {
            Duration = effectiveExpiration,
            IsFailSafeEnabled = cacheOptions.IsFailSafeEnabled,
            FailSafeMaxDuration = cacheOptions.FailSafeMaxDuration,
            FailSafeThrottleDuration = cacheOptions.FailSafeThrottleDuration,
            JitterMaxDuration = cacheOptions.JitterMaxDuration,
            AllowBackgroundDistributedCacheOperations = false,
            AllowBackgroundBackplaneOperations = false,
            ReThrowDistributedCacheExceptions = false,
            ReThrowSerializationExceptions = true,
            ReThrowBackplaneExceptions = false,
            SkipMemoryCacheRead = true,
            SkipMemoryCacheWrite = true
        };

        if (cacheOptions.EagerRefreshThreshold > 0)
        {
            defaultEntryOptions.EagerRefreshThreshold = cacheOptions.EagerRefreshThreshold;
        }

        var fusion = new FusionCache(
            Options.Create(new FusionCacheOptions
            {
                CacheName = $"KontentDelivery.Distributed.{(string.IsNullOrWhiteSpace(keyPrefix) ? "Default" : keyPrefix)}",
                DistributedCacheKeyModifierMode = CacheKeyModifierMode.None,
                DefaultEntryOptions = defaultEntryOptions
            }),
            memoryCache: null,
            logger: null);

        var serializer = new FusionCacheSystemTextJsonSerializer(serializerOptions);
        fusion.SetupDistributedCache(distributedCache, serializer);

        var baseWriteOptions = new FusionCacheEntryOptions
        {
            Duration = effectiveExpiration,
            IsFailSafeEnabled = cacheOptions.IsFailSafeEnabled,
            FailSafeMaxDuration = cacheOptions.FailSafeMaxDuration,
            FailSafeThrottleDuration = cacheOptions.FailSafeThrottleDuration,
            JitterMaxDuration = cacheOptions.JitterMaxDuration,
            SkipMemoryCacheRead = false,
            SkipMemoryCacheWrite = false,
            SkipDistributedCacheRead = false,
            SkipDistributedCacheWrite = false,
            ReThrowDistributedCacheExceptions = true,
            ReThrowSerializationExceptions = true,
            ReThrowBackplaneExceptions = false,
            AllowBackgroundBackplaneOperations = false,
            AllowBackgroundDistributedCacheOperations = false
        };

        if (cacheOptions.EagerRefreshThreshold > 0)
        {
            baseWriteOptions.EagerRefreshThreshold = cacheOptions.EagerRefreshThreshold;
        }

        return new FusionCacheManager(
            fusion,
            CacheStorageMode.RawJson,
            effectiveExpiration,
            cacheKey => $"{prefixSegment}cache:{cacheKey}",
            dependency => $"{prefixSegment}dep:{dependency}",
            logger,
            baseWriteOptions: baseWriteOptions,
            baseInvalidateOptions: new FusionCacheEntryOptions
            {
                IsFailSafeEnabled = false,
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

    public async Task<T?> GetOrSetAsync<T>(
        string cacheKey,
        Func<CancellationToken, Task<CacheEntry<T>?>> factory,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
        where T : class
    {
        ThrowIfDisposed();

        if (string.IsNullOrWhiteSpace(cacheKey))
        {
            var entry = await factory(cancellationToken).ConfigureAwait(false);
            return entry?.Value;
        }

        var formattedKey = _cacheKeyFormatter(cacheKey);
        var factoryCalled = false;
        var factoryReturnedNull = false;

        var result = await _cache.GetOrSetAsync<T>(
            formattedKey,
            async (ctx, ct) =>
            {
                factoryCalled = true;
                var factoryResult = await factory(ct).ConfigureAwait(false);
                if (factoryResult is null)
                {
                    factoryReturnedNull = true;
                    ctx.Options.SkipMemoryCacheWrite = true;
                    ctx.Options.SkipDistributedCacheWrite = true;
                    return default!;
                }

                var tags = factoryResult.Dependencies
                    .Where(d => !string.IsNullOrWhiteSpace(d))
                    .Select(_dependencyTagFormatter)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();
                ctx.Tags = tags;
                ctx.Options.Duration = expiration ?? _defaultExpiration;
                return factoryResult.Value;
            },
            _baseWriteOptions,
            token: cancellationToken).ConfigureAwait(false);

        // Cache hit (factory never called): return the cached result.
        // Factory called and returned null: return null (don't cache).
        // Factory called with value: return the result.
        return factoryCalled && factoryReturnedNull ? null : result;
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

    public async Task PurgeAsync(bool allowFailSafe = false, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();

        await _cache.ClearAsync(
                allowFailSafe,
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
