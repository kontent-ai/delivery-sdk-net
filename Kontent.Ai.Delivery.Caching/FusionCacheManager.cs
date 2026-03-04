using System.Collections.Concurrent;
using System.Text.Json;
using Kontent.Ai.Delivery.Logging;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Events;
using ZiggyCreatures.Caching.Fusion.Serialization.SystemTextJson;

namespace Kontent.Ai.Delivery.Caching;

/// <summary>
/// Shared FusionCache-backed implementation of SDK cache manager behavior.
/// </summary>
internal sealed class FusionCacheManager : IDeliveryCacheManager, IDeliveryCachePurger, IFailSafeStateProvider, IDisposable
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
    private readonly ConcurrentDictionary<string, byte> _failSafeActiveKeys = new(StringComparer.Ordinal);
    private readonly EventHandler<FusionCacheEntryEventArgs> _failSafeActivateHandler;
    private readonly EventHandler<FusionCacheEntryEventArgs> _factorySuccessHandler;
    private readonly EventHandler<FusionCacheEntryHitEventArgs> _hitHandler;
    private readonly EventHandler<FusionCacheEntryEventArgs> _removeHandler;
    private readonly EventHandler<FusionCacheEntryEvictionEventArgs> _evictionHandler;
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

        _failSafeActivateHandler = HandleFailSafeActivate;
        _factorySuccessHandler = HandleFactorySuccess;
        _hitHandler = HandleHit;
        _removeHandler = HandleRemove;
        _evictionHandler = HandleEviction;
        SubscribeFailSafeStateEvents();
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

        var fusionCacheOptions = new FusionCacheOptions
        {
            CacheName = $"KontentDelivery.Memory.{(string.IsNullOrWhiteSpace(keyPrefix) ? "Default" : keyPrefix)}",
            DistributedCacheKeyModifierMode = CacheKeyModifierMode.None,
            // Required for deterministic fail-safe source propagation in query builders.
            EnableSyncEventHandlersExecution = true,
            DefaultEntryOptions = defaultEntryOptions
        };

        cacheOptions.ConfigureFusionCacheOptions?.Invoke(fusionCacheOptions);

        var fusion = new FusionCache(
            Options.Create(fusionCacheOptions),
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

    public static FusionCacheManager CreateHybrid(
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

        var fusionCacheOptions = new FusionCacheOptions
        {
            CacheName = $"KontentDelivery.Hybrid.{(string.IsNullOrWhiteSpace(keyPrefix) ? "Default" : keyPrefix)}",
            DistributedCacheKeyModifierMode = CacheKeyModifierMode.None,
            // Required for deterministic fail-safe source propagation in query builders.
            EnableSyncEventHandlersExecution = true,
            DefaultEntryOptions = defaultEntryOptions
        };

        cacheOptions.ConfigureFusionCacheOptions?.Invoke(fusionCacheOptions);

        var fusion = new FusionCache(
            Options.Create(fusionCacheOptions),
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

        try
        {
            return await _cache.GetOrSetAsync<T>(
                formattedKey,
                async (ctx, ct) =>
                {
                    var factoryResult = await factory(ct).ConfigureAwait(false);

                    if (factoryResult is null)
                    {
                        throw new CacheFactoryFailedException();
                    }

                    var tags = factoryResult.Dependencies
                        .Where(d => !string.IsNullOrWhiteSpace(d))
                        .Select(_dependencyTagFormatter)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToArray();
                    ctx.Tags = tags;
                    ctx.Options.Duration = expiration ?? _defaultExpiration;
                    _failSafeActiveKeys.TryRemove(formattedKey, out var _);
                    _failSafeActiveKeys.TryRemove(cacheKey, out var _);
                    return factoryResult.Value;
                },
                _baseWriteOptions,
                token: cancellationToken).ConfigureAwait(false);
        }
        catch (CacheFactoryFailedException)
        {
            // Factory returned null and no stale entry was available for fail-safe.
            _failSafeActiveKeys.TryRemove(formattedKey, out var _);
            return null;
        }
        catch
        {
            // Factory threw and no stale entry was available for fail-safe.
            _failSafeActiveKeys.TryRemove(formattedKey, out var _);
            throw;
        }
    }

    /// <summary>
    /// Sentinel exception thrown inside the FusionCache factory when the upstream
    /// factory returns <c>null</c>.  This allows FusionCache fail-safe to kick in
    /// and serve a stale entry when one is available.  The exception never leaves
    /// <see cref="GetOrSetAsync{T}"/> — it is caught immediately after the
    /// <c>GetOrSetAsync</c> call.
    /// </summary>
#pragma warning disable S3871 // Intentionally private sentinel — never leaves this class
    private sealed class CacheFactoryFailedException : Exception;
#pragma warning restore S3871

    public async Task<bool> InvalidateAsync(CancellationToken cancellationToken = default, params string[] dependencyKeys)
    {
        ThrowIfDisposed();

        if (dependencyKeys is null || dependencyKeys.Length == 0)
        {
            return true;
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

            return true;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            if (_logger is not null)
                LoggerMessages.CacheInvalidationFailed(_logger, ex);

            return false;
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

        // Only clear fail-safe tracking when entries are permanently removed.
        // When allowFailSafe is true, entries remain for fail-safe and should
        // continue to be reported as ResponseSource.FailSafe.
        if (!allowFailSafe)
        {
            _failSafeActiveKeys.Clear();
        }
    }

    bool IFailSafeStateProvider.IsFailSafeActive(string cacheKey)
    {
        if (string.IsNullOrWhiteSpace(cacheKey))
        {
            return false;
        }

        return _failSafeActiveKeys.ContainsKey(cacheKey)
            || _failSafeActiveKeys.ContainsKey(_cacheKeyFormatter(cacheKey));
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposeState, 1) != 0)
            return;

        UnsubscribeFailSafeStateEvents();

        if (_ownsFusionCache)
        {
            _cache.Dispose();
        }
    }

    private void ThrowIfDisposed() =>
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposeState) != 0, nameof(FusionCacheManager));

    private void SubscribeFailSafeStateEvents()
    {
        _cache.Events.FailSafeActivate += _failSafeActivateHandler;
        _cache.Events.FactorySuccess += _factorySuccessHandler;
        _cache.Events.Hit += _hitHandler;
        _cache.Events.Remove += _removeHandler;
        _cache.Events.Memory.Eviction += _evictionHandler;
    }

    private void UnsubscribeFailSafeStateEvents()
    {
        _cache.Events.FailSafeActivate -= _failSafeActivateHandler;
        _cache.Events.FactorySuccess -= _factorySuccessHandler;
        _cache.Events.Hit -= _hitHandler;
        _cache.Events.Remove -= _removeHandler;
        _cache.Events.Memory.Eviction -= _evictionHandler;
    }

    private void HandleFailSafeActivate(object? sender, FusionCacheEntryEventArgs eventArgs)
        => _failSafeActiveKeys[eventArgs.Key] = 1;

    private void HandleFactorySuccess(object? sender, FusionCacheEntryEventArgs eventArgs)
        => _failSafeActiveKeys.TryRemove(eventArgs.Key, out var _);

    private void HandleHit(object? sender, FusionCacheEntryHitEventArgs eventArgs)
    {
        if (eventArgs.IsStale)
        {
            _failSafeActiveKeys[eventArgs.Key] = 1;
            return;
        }

        _failSafeActiveKeys.TryRemove(eventArgs.Key, out var _);
    }

    private void HandleRemove(object? sender, FusionCacheEntryEventArgs eventArgs)
        => _failSafeActiveKeys.TryRemove(eventArgs.Key, out var _);

    private void HandleEviction(object? sender, FusionCacheEntryEvictionEventArgs eventArgs)
        => _failSafeActiveKeys.TryRemove(eventArgs.Key, out var _);
}
