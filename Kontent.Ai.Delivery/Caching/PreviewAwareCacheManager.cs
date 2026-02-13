using Kontent.Ai.Delivery.Configuration;
using Microsoft.Extensions.Options;

namespace Kontent.Ai.Delivery.Caching;

/// <summary>
/// Decorates a cache manager and bypasses cache reads/writes for preview clients.
/// </summary>
internal sealed class PreviewAwareCacheManager(
    IDeliveryCacheManager inner,
    IOptionsMonitor<DeliveryOptions> optionsMonitor,
    string clientName) : IDeliveryCacheManager
{
    private readonly IDeliveryCacheManager _inner = inner;
    private readonly IOptionsMonitor<DeliveryOptions> _optionsMonitor = optionsMonitor;
    private readonly string _clientName = clientName;

    public CacheStorageMode StorageMode => _inner.StorageMode;

    public Task<T?> GetAsync<T>(string cacheKey, CancellationToken cancellationToken = default) where T : class => IsPreviewClient() ? Task.FromResult<T?>(null) : _inner.GetAsync<T>(cacheKey, cancellationToken);

    public Task SetAsync<T>(
        string cacheKey,
        T value,
        IEnumerable<string> dependencies,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default) where T : class => IsPreviewClient() ? Task.CompletedTask : _inner.SetAsync(cacheKey, value, dependencies, expiration, cancellationToken);

    public Task InvalidateAsync(CancellationToken cancellationToken = default, params string[] dependencyKeys)
        => _inner.InvalidateAsync(cancellationToken, dependencyKeys);

    private bool IsPreviewClient() => _optionsMonitor.Get(_clientName).UsePreviewApi;
}
