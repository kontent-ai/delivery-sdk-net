using System.Reflection;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.Configuration;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Kontent.Ai.Delivery.Tests.Builders.Configuration;

public class DeliveryClientBuilderTests
{
    private const string EnvironmentId = "550cec62-90a6-4ab3-b3e4-3d0bb4c04f5c";
    private const string TestPreviewApiKey = "preview.api.key";
    private const string TestSecureApiKey = "secure.api.key";
    private static readonly Guid EnvironmentIdGuid = Guid.Parse(EnvironmentId);

    [Fact]
    public void Build_WithOptions_CreatesClient()
    {
        // Act
        using var container = DeliveryClientBuilder
            .WithOptions(builder => builder
                .WithEnvironmentId(EnvironmentId)
                .UseProductionApi()
                .Build())
            .Build();

        // Assert
        Assert.NotNull(container);
        Assert.IsAssignableFrom<IDeliveryClientContainer>(container);
        Assert.NotNull(container.Client);
        Assert.IsAssignableFrom<IDeliveryClient>(container.Client);
    }

    [Fact]
    public void Build_WithOptionsGuid_CreatesClient()
    {
        // Act
        using var container = DeliveryClientBuilder
            .WithOptions(builder => builder
                .WithEnvironmentId(EnvironmentIdGuid)
                .UseProductionApi()
                .Build())
            .Build();

        // Assert
        Assert.NotNull(container);
        Assert.IsAssignableFrom<IDeliveryClientContainer>(container);
        Assert.NotNull(container.Client);
        Assert.IsAssignableFrom<IDeliveryClient>(container.Client);
    }

    [Fact]
    public void Build_WithPreviewApi_CreatesClient()
    {
        // Act
        using var container = DeliveryClientBuilder
            .WithOptions(builder => builder
                .WithEnvironmentId(EnvironmentId)
                .UsePreviewApi(TestPreviewApiKey)
                .Build())
            .Build();

        // Assert
        Assert.NotNull(container);
        Assert.NotNull(container.Client);
    }

    [Fact]
    public void Build_WithTypeProvider_CreatesClient()
    {
        // Arrange
        var typeProvider = new TestTypeProvider();

        // Act
        using var container = DeliveryClientBuilder
            .WithOptions(builder => builder
                .WithEnvironmentId(EnvironmentId)
                .UseProductionApi()
                .Build())
            .WithTypeProvider(typeProvider)
            .Build();

        // Assert
        Assert.NotNull(container);
        Assert.NotNull(container.Client);
    }

    [Fact]
    public void Build_WithMemoryCache_CreatesClient()
    {
        // Act
        using var container = DeliveryClientBuilder
            .WithOptions(builder => builder
                .WithEnvironmentId(EnvironmentId)
                .UseProductionApi()
                .Build())
            .WithMemoryCache(TimeSpan.FromMinutes(30))
            .Build();

        // Assert
        Assert.NotNull(container);
        Assert.NotNull(container.Client);
    }

    [Fact]
    public void Build_WithMemoryCacheDefaultExpiration_CreatesClient()
    {
        // Act
        using var container = DeliveryClientBuilder
            .WithOptions(builder => builder
                .WithEnvironmentId(EnvironmentId)
                .UseProductionApi()
                .Build())
            .WithMemoryCache()
            .Build();

        // Assert
        Assert.NotNull(container);
        Assert.NotNull(container.Client);
    }

    [Fact]
    public async Task Build_WithPreviewApiAndMemoryCache_CacheManagerStoresAndReads()
    {
        using var container = DeliveryClientBuilder
            .WithOptions(builder => builder
                .WithEnvironmentId(EnvironmentId)
                .UsePreviewApi(TestPreviewApiKey)
                .Build())
            .WithMemoryCache(TimeSpan.FromMinutes(30))
            .Build();

        var cacheManager = GetCacheManager(container.Client);
        Assert.NotNull(cacheManager);

        var factoryCalled = false;
        var cached = await cacheManager.GetOrSetAsync("preview-test", _ =>
        {
            factoryCalled = true;
            return Task.FromResult<CacheEntry<string>?>(
                new CacheEntry<string>("value", ["item_preview"]));
        });

        Assert.True(factoryCalled);
        Assert.Equal("value", cached);
    }

    [Fact]
    public async Task Build_WithProductionApiAndMemoryCache_CacheManagerStoresAndReads()
    {
        using var container = DeliveryClientBuilder
            .WithOptions(builder => builder
                .WithEnvironmentId(EnvironmentId)
                .UseProductionApi()
                .Build())
            .WithMemoryCache(TimeSpan.FromMinutes(30))
            .Build();

        var cacheManager = GetCacheManager(container.Client);
        Assert.NotNull(cacheManager);

        var factoryCalled = false;
        var cached = await cacheManager.GetOrSetAsync("production-test", _ =>
        {
            factoryCalled = true;
            return Task.FromResult<CacheEntry<string>?>(
                new CacheEntry<string>("value", ["item_production"]));
        });

        Assert.True(factoryCalled);
        Assert.Equal("value", cached);
    }

    [Fact]
    public void Build_WithDistributedCache_CreatesClient()
    {
        // Arrange
        var distributedCache = new TestDistributedCache();

        // Act
        using var container = DeliveryClientBuilder
            .WithOptions(builder => builder
                .WithEnvironmentId(EnvironmentId)
                .UseProductionApi()
                .Build())
            .WithDistributedCache(distributedCache, TimeSpan.FromHours(1))
            .Build();

        // Assert
        Assert.NotNull(container);
        Assert.NotNull(container.Client);
    }

    [Fact]
    public void Build_WithAllOptions_CreatesClient()
    {
        // Arrange
        var typeProvider = new TestTypeProvider();

        // Act
        using var container = DeliveryClientBuilder
            .WithOptions(builder => builder
                .WithEnvironmentId(EnvironmentId)
                .UseProductionApi()
                .WithDefaultRenditionPreset("mobile")
                .Build())
            .WithTypeProvider(typeProvider)
            .WithMemoryCache(TimeSpan.FromMinutes(15))
            .Build();

        // Assert
        Assert.NotNull(container);
        Assert.NotNull(container.Client);
    }

    [Fact]
    public void WithOptions_WithNullDelegate_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            DeliveryClientBuilder.WithOptions(null!));
    }

    [Fact]
    public void WithTypeProvider_WithNull_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            DeliveryClientBuilder
                .WithOptions(builder => builder
                    .WithEnvironmentId(EnvironmentId)
                    .UseProductionApi()
                    .Build())
                .WithTypeProvider(null!));
    }

    [Fact]
    public void WithDistributedCache_WithNull_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            DeliveryClientBuilder
                .WithOptions(builder => builder
                    .WithEnvironmentId(EnvironmentId)
                    .UseProductionApi()
                    .Build())
                .WithDistributedCache(null!));
    }

    [Fact]
    public void Build_CallingMemoryCacheAfterDistributedCache_UsesLastConfigured()
    {
        // Arrange
        var distributedCache = new TestDistributedCache();

        // Act - last cache type wins
        using var container = DeliveryClientBuilder
            .WithOptions(builder => builder
                .WithEnvironmentId(EnvironmentId)
                .UseProductionApi()
                .Build())
            .WithDistributedCache(distributedCache)
            .WithMemoryCache(TimeSpan.FromMinutes(30))
            .Build();

        // Assert
        Assert.NotNull(container);
        Assert.NotNull(container.Client);
    }

    [Fact]
    public void Build_CallingDistributedCacheAfterMemoryCache_UsesLastConfigured()
    {
        // Arrange
        var distributedCache = new TestDistributedCache();

        // Act - last cache type wins
        using var container = DeliveryClientBuilder
            .WithOptions(builder => builder
                .WithEnvironmentId(EnvironmentId)
                .UseProductionApi()
                .Build())
            .WithMemoryCache(TimeSpan.FromMinutes(30))
            .WithDistributedCache(distributedCache, TimeSpan.FromHours(1))
            .Build();

        // Assert
        Assert.NotNull(container);
        Assert.NotNull(container.Client);
    }

    [Fact]
    public void Build_FluentChaining_ReturnsSameBuilderInstance()
    {
        // Arrange
        var typeProvider = new TestTypeProvider();

        // Act
        var builder1 = DeliveryClientBuilder
            .WithOptions(builder => builder
                .WithEnvironmentId(EnvironmentId)
                .UseProductionApi()
                .Build());
        var builder2 = builder1.WithTypeProvider(typeProvider);
        var builder3 = builder2.WithMemoryCache();

        // Assert - all should be the same builder instance for proper fluent chaining
        Assert.Same(builder1, builder2);
        Assert.Same(builder2, builder3);
    }

    [Fact]
    public void Build_MultipleClients_AreIndependent()
    {
        // Act
        using var container1 = DeliveryClientBuilder
            .WithOptions(builder => builder
                .WithEnvironmentId(EnvironmentId)
                .UseProductionApi()
                .Build())
            .Build();

        using var container2 = DeliveryClientBuilder
            .WithOptions(builder => builder
                .WithEnvironmentId(EnvironmentId)
                .UseProductionApi()
                .Build())
            .WithMemoryCache()
            .Build();

        // Assert
        Assert.NotNull(container1);
        Assert.NotNull(container2);
        Assert.NotSame(container1, container2);
        Assert.NotSame(container1.Client, container2.Client);
    }

    [Fact]
    public void Build_WithSecureAccess_CreatesClient()
    {
        // Act
        using var container = DeliveryClientBuilder
            .WithOptions(builder => builder
                .WithEnvironmentId(EnvironmentId)
                .UseProductionApi(TestSecureApiKey)
                .Build())
            .Build();

        // Assert
        Assert.NotNull(container);
        Assert.NotNull(container.Client);
    }

    [Fact]
    public void Build_WithDisabledResilience_CreatesClient()
    {
        // Act
        using var container = DeliveryClientBuilder
            .WithOptions(builder => builder
                .WithEnvironmentId(EnvironmentId)
                .UseProductionApi()
                .DisableRetryPolicy()
                .Build())
            .Build();

        // Assert
        Assert.NotNull(container);
        Assert.NotNull(container.Client);
    }

    [Fact]
    public void Build_WithoutOptions_ThrowsInvalidOperationException()
    {
        // Use reflection to create a builder without calling WithOptions
        var builder = (DeliveryClientBuilder)Activator.CreateInstance(
            typeof(DeliveryClientBuilder),
            BindingFlags.NonPublic | BindingFlags.Instance,
            null, [], null)!;

        Assert.Throws<InvalidOperationException>(() => builder.Build());
    }

    [Fact]
    public void Build_WithLoggerFactory_CreatesClient()
    {
        using var loggerFactory = LoggerFactory.Create(b => { });

        using var container = DeliveryClientBuilder
            .WithOptions(builder => builder
                .WithEnvironmentId(EnvironmentId)
                .UseProductionApi()
                .Build())
            .WithLoggerFactory(loggerFactory)
            .Build();

        Assert.NotNull(container);
        Assert.NotNull(container.Client);
    }

    [Fact]
    public void WithLoggerFactory_WithNull_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            DeliveryClientBuilder
                .WithOptions(builder => builder
                    .WithEnvironmentId(EnvironmentId)
                    .UseProductionApi()
                    .Build())
                .WithLoggerFactory(null!));
    }

    [Fact]
    public void ConfigureServices_WithNull_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            DeliveryClientBuilder
                .WithOptions(builder => builder
                    .WithEnvironmentId(EnvironmentId)
                    .UseProductionApi()
                    .Build())
                .ConfigureServices(null!));
    }

    [Fact]
    public void Build_WithMemoryCacheAdvanced_CreatesClient()
    {
        // Act
        using var container = DeliveryClientBuilder
            .WithOptions(builder => builder
                .WithEnvironmentId(EnvironmentId)
                .UseProductionApi()
                .Build())
            .WithMemoryCache(opts =>
            {
                opts.DefaultExpiration = TimeSpan.FromMinutes(15);
                opts.IsFailSafeEnabled = true;
            })
            .Build();

        // Assert
        Assert.NotNull(container);
        Assert.IsAssignableFrom<IDeliveryClientContainer>(container);
        Assert.NotNull(container.Client);
        Assert.IsAssignableFrom<IDeliveryClient>(container.Client);
    }

    [Fact]
    public void Build_WithDistributedCacheAdvanced_CreatesClient()
    {
        // Arrange
        var distributedCache = new TestDistributedCache();

        // Act
        using var container = DeliveryClientBuilder
            .WithOptions(builder => builder
                .WithEnvironmentId(EnvironmentId)
                .UseProductionApi()
                .Build())
            .WithDistributedCache(distributedCache, opts =>
            {
                opts.DefaultExpiration = TimeSpan.FromMinutes(30);
            })
            .Build();

        // Assert
        Assert.NotNull(container);
        Assert.IsAssignableFrom<IDeliveryClientContainer>(container);
        Assert.NotNull(container.Client);
        Assert.IsAssignableFrom<IDeliveryClient>(container.Client);
    }

    [Fact]
    public void WithMemoryCacheAdvanced_WithNullDelegate_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            DeliveryClientBuilder
                .WithOptions(builder => builder
                    .WithEnvironmentId(EnvironmentId)
                    .UseProductionApi()
                    .Build())
                .WithMemoryCache((Action<DeliveryCacheOptions>)null!));
    }

    [Fact]
    public void WithDistributedCacheAdvanced_WithNullDistributedCache_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            DeliveryClientBuilder
                .WithOptions(builder => builder
                    .WithEnvironmentId(EnvironmentId)
                    .UseProductionApi()
                    .Build())
                .WithDistributedCache(null!, opts =>
                {
                    opts.DefaultExpiration = TimeSpan.FromMinutes(30);
                }));
    }

    [Fact]
    public void WithDistributedCacheAdvanced_WithNullDelegate_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            DeliveryClientBuilder
                .WithOptions(builder => builder
                    .WithEnvironmentId(EnvironmentId)
                    .UseProductionApi()
                    .Build())
                .WithDistributedCache(new TestDistributedCache(), (Action<DeliveryCacheOptions>)null!));
    }

    // Simple test implementation of ITypeProvider
    private class TestTypeProvider : ITypeProvider
    {
        public Type? GetType(string contentType) => null;
        public string? GetCodename(Type contentType) => null;
    }

    private static IDeliveryCacheManager? GetCacheManager(IDeliveryClient client)
    {
        var cacheManagerField = client.GetType()
            .GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
            .FirstOrDefault(f => typeof(IDeliveryCacheManager).IsAssignableFrom(f.FieldType));
        return cacheManagerField?.GetValue(client) as IDeliveryCacheManager;
    }

    // Simple test implementation of IDistributedCache
    private class TestDistributedCache : IDistributedCache
    {
        private readonly Dictionary<string, byte[]> _cache = [];

        public byte[]? Get(string key) =>
            _cache.TryGetValue(key, out var value) ? value : null;

        public Task<byte[]?> GetAsync(string key, CancellationToken token = default) =>
            Task.FromResult(Get(key));

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options) =>
            _cache[key] = value;

        public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
        {
            Set(key, value, options);
            return Task.CompletedTask;
        }

        public void Refresh(string key) { }

        public Task RefreshAsync(string key, CancellationToken token = default) =>
            Task.CompletedTask;

        public void Remove(string key) =>
            _cache.Remove(key);

        public Task RemoveAsync(string key, CancellationToken token = default)
        {
            Remove(key);
            return Task.CompletedTask;
        }
    }
}
