using System.Reflection;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.Configuration;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Kontent.Ai.Delivery.Tests.Builders.Configuration;

public class DeliveryClientBuilderTests
{
    private const string EnvironmentId = "550cec62-90a6-4ab3-b3e4-3d0bb4c04f5c";
    private const string TestPreviewApiKey = "preview.api.key";
    private const string TestSecureApiKey = "secure.api.key";
    private static readonly Guid EnvironmentIdGuid = Guid.Parse(EnvironmentId);

    [Fact]
    public async Task Build_WithOptions_CreatesClient()
    {
        // Act
        await using var client = DeliveryClientBuilder
            .WithOptions(builder => builder
                .WithEnvironmentId(EnvironmentId)
                .UseProductionApi()
                .Build())
            .Build();

        // Assert
        Assert.NotNull(client);
        Assert.IsAssignableFrom<IDeliveryClient>(client);
    }

    [Fact]
    public async Task Build_WithOptionsGuid_CreatesClient()
    {
        // Act
        await using var client = DeliveryClientBuilder
            .WithOptions(builder => builder
                .WithEnvironmentId(EnvironmentIdGuid)
                .UseProductionApi()
                .Build())
            .Build();

        // Assert
        Assert.NotNull(client);
        Assert.IsAssignableFrom<IDeliveryClient>(client);
    }

    [Fact]
    public async Task Build_WithPreviewApi_CreatesClient()
    {
        // Act
        await using var client = DeliveryClientBuilder
            .WithOptions(builder => builder
                .WithEnvironmentId(EnvironmentId)
                .UsePreviewApi(TestPreviewApiKey)
                .Build())
            .Build();

        // Assert
        Assert.NotNull(client);
    }

    [Fact]
    public async Task Build_WithTypeProvider_CreatesClient()
    {
        // Arrange
        var typeProvider = new TestTypeProvider();

        // Act
        await using var client = DeliveryClientBuilder
            .WithOptions(builder => builder
                .WithEnvironmentId(EnvironmentId)
                .UseProductionApi()
                .Build())
            .WithTypeProvider(typeProvider)
            .Build();

        // Assert
        Assert.NotNull(client);
    }

    [Fact]
    public async Task Build_WithMemoryCache_CreatesClient()
    {
        // Act
        await using var client = DeliveryClientBuilder
            .WithOptions(builder => builder
                .WithEnvironmentId(EnvironmentId)
                .UseProductionApi()
                .Build())
            .WithMemoryCache(TimeSpan.FromMinutes(30))
            .Build();

        // Assert
        Assert.NotNull(client);
    }

    [Fact]
    public async Task Build_WithMemoryCacheDefaultExpiration_CreatesClient()
    {
        // Act
        await using var client = DeliveryClientBuilder
            .WithOptions(builder => builder
                .WithEnvironmentId(EnvironmentId)
                .UseProductionApi()
                .Build())
            .WithMemoryCache()
            .Build();

        // Assert
        Assert.NotNull(client);
    }

    [Fact]
    public async Task Build_WithPreviewApiAndMemoryCache_CacheManagerStoresAndReads()
    {
        await using var client = DeliveryClientBuilder
            .WithOptions(builder => builder
                .WithEnvironmentId(EnvironmentId)
                .UsePreviewApi(TestPreviewApiKey)
                .Build())
            .WithMemoryCache(TimeSpan.FromMinutes(30))
            .Build();

        var cacheManager = GetCacheManager(client);
        Assert.NotNull(cacheManager);

        var factoryCalled = false;
        var cached = await cacheManager.GetOrSetAsync("preview-test", _ =>
        {
            factoryCalled = true;
            return Task.FromResult<CacheEntry<string>?>(
                new CacheEntry<string>("value", ["item_preview"]));
        });

        Assert.True(factoryCalled);
        Assert.Equal("value", cached?.Value);
    }

    [Fact]
    public async Task Build_WithProductionApiAndMemoryCache_CacheManagerStoresAndReads()
    {
        await using var client = DeliveryClientBuilder
            .WithOptions(builder => builder
                .WithEnvironmentId(EnvironmentId)
                .UseProductionApi()
                .Build())
            .WithMemoryCache(TimeSpan.FromMinutes(30))
            .Build();

        var cacheManager = GetCacheManager(client);
        Assert.NotNull(cacheManager);

        var factoryCalled = false;
        var cached = await cacheManager.GetOrSetAsync("production-test", _ =>
        {
            factoryCalled = true;
            return Task.FromResult<CacheEntry<string>?>(
                new CacheEntry<string>("value", ["item_production"]));
        });

        Assert.True(factoryCalled);
        Assert.Equal("value", cached?.Value);
    }

    [Fact]
    public async Task Build_WithHybridCache_CreatesClient()
    {
        // Arrange
        var distributedCache = new TestDistributedCache();

        // Act
        await using var client = DeliveryClientBuilder
            .WithOptions(builder => builder
                .WithEnvironmentId(EnvironmentId)
                .UseProductionApi()
                .Build())
            .WithHybridCache(distributedCache, TimeSpan.FromHours(1))
            .Build();

        // Assert
        Assert.NotNull(client);
    }

    [Fact]
    public async Task Build_WithAllOptions_CreatesClient()
    {
        // Arrange
        var typeProvider = new TestTypeProvider();

        // Act
        await using var client = DeliveryClientBuilder
            .WithOptions(builder => builder
                .WithEnvironmentId(EnvironmentId)
                .UseProductionApi()
                .WithDefaultRenditionPreset("mobile")
                .Build())
            .WithTypeProvider(typeProvider)
            .WithMemoryCache(TimeSpan.FromMinutes(15))
            .Build();

        // Assert
        Assert.NotNull(client);
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
    public void WithHybridCache_WithNull_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            DeliveryClientBuilder
                .WithOptions(builder => builder
                    .WithEnvironmentId(EnvironmentId)
                    .UseProductionApi()
                    .Build())
                .WithHybridCache(null!));
    }

    [Fact]
    public async Task Build_CallingMemoryCacheAfterHybridCache_UsesLastConfigured()
    {
        // Arrange
        var distributedCache = new TestDistributedCache();

        // Act - last cache type wins
        await using var client = DeliveryClientBuilder
            .WithOptions(builder => builder
                .WithEnvironmentId(EnvironmentId)
                .UseProductionApi()
                .Build())
            .WithHybridCache(distributedCache)
            .WithMemoryCache(TimeSpan.FromMinutes(30))
            .Build();

        // Assert
        Assert.NotNull(client);
    }

    [Fact]
    public async Task Build_CallingHybridCacheAfterMemoryCache_UsesLastConfigured()
    {
        // Arrange
        var distributedCache = new TestDistributedCache();

        // Act - last cache type wins
        await using var client = DeliveryClientBuilder
            .WithOptions(builder => builder
                .WithEnvironmentId(EnvironmentId)
                .UseProductionApi()
                .Build())
            .WithMemoryCache(TimeSpan.FromMinutes(30))
            .WithHybridCache(distributedCache, TimeSpan.FromHours(1))
            .Build();

        // Assert
        Assert.NotNull(client);
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
    public async Task Build_MultipleClients_AreIndependent()
    {
        // Act
        await using var client1 = DeliveryClientBuilder
            .WithOptions(builder => builder
                .WithEnvironmentId(EnvironmentId)
                .UseProductionApi()
                .Build())
            .Build();

        await using var client2 = DeliveryClientBuilder
            .WithOptions(builder => builder
                .WithEnvironmentId(EnvironmentId)
                .UseProductionApi()
                .Build())
            .WithMemoryCache()
            .Build();

        // Assert
        Assert.NotNull(client1);
        Assert.NotNull(client2);
        Assert.NotSame(client1, client2);
    }

    [Fact]
    public async Task Build_WithSecureAccess_CreatesClient()
    {
        // Act
        await using var client = DeliveryClientBuilder
            .WithOptions(builder => builder
                .WithEnvironmentId(EnvironmentId)
                .UseProductionApi(TestSecureApiKey)
                .Build())
            .Build();

        // Assert
        Assert.NotNull(client);
    }

    [Fact]
    public async Task Build_WithDisabledResilience_CreatesClient()
    {
        // Act
        await using var client = DeliveryClientBuilder
            .WithOptions(builder => builder
                .WithEnvironmentId(EnvironmentId)
                .UseProductionApi()
                .DisableRetryPolicy()
                .Build())
            .Build();

        // Assert
        Assert.NotNull(client);
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
    public async Task Build_WithLoggerFactory_CreatesClient()
    {
        using var loggerFactory = LoggerFactory.Create(b => { });

        await using var client = DeliveryClientBuilder
            .WithOptions(builder => builder
                .WithEnvironmentId(EnvironmentId)
                .UseProductionApi()
                .Build())
            .WithLoggerFactory(loggerFactory)
            .Build();

        Assert.NotNull(client);
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
    public async Task Build_WithMemoryCacheAdvanced_CreatesClient()
    {
        // Act
        await using var client = DeliveryClientBuilder
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
        Assert.NotNull(client);
        Assert.IsAssignableFrom<IDeliveryClient>(client);
    }

    [Fact]
    public async Task Build_WithHybridCacheAdvanced_CreatesClient()
    {
        // Arrange
        var distributedCache = new TestDistributedCache();

        // Act
        await using var client = DeliveryClientBuilder
            .WithOptions(builder => builder
                .WithEnvironmentId(EnvironmentId)
                .UseProductionApi()
                .Build())
            .WithHybridCache(distributedCache, opts =>
            {
                opts.DefaultExpiration = TimeSpan.FromMinutes(30);
            })
            .Build();

        // Assert
        Assert.NotNull(client);
        Assert.IsAssignableFrom<IDeliveryClient>(client);
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
    public void WithHybridCacheAdvanced_WithNullDistributedCache_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            DeliveryClientBuilder
                .WithOptions(builder => builder
                    .WithEnvironmentId(EnvironmentId)
                    .UseProductionApi()
                    .Build())
                .WithHybridCache(null!, opts =>
                {
                    opts.DefaultExpiration = TimeSpan.FromMinutes(30);
                }));
    }

    [Fact]
    public void WithHybridCacheAdvanced_WithNullDelegate_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            DeliveryClientBuilder
                .WithOptions(builder => builder
                    .WithEnvironmentId(EnvironmentId)
                    .UseProductionApi()
                    .Build())
                .WithHybridCache(new TestDistributedCache(), (Action<DeliveryCacheOptions>)null!));
    }

    private sealed class BuilderSiblingOptions
    {
        public TimeSpan CacheExpiration { get; set; } = TimeSpan.FromMinutes(5);
    }

    [Fact]
    public async Task Build_WithMemoryCache_ServiceProviderCallback_InvokesCallbackOnResolution()
    {
        var invokedWithExpiration = TimeSpan.Zero;

        await using var client = DeliveryClientBuilder
            .WithOptions(builder => builder
                .WithEnvironmentId(EnvironmentId)
                .UseProductionApi()
                .Build())
            .ConfigureServices(services => services.Configure<BuilderSiblingOptions>(o => o.CacheExpiration = TimeSpan.FromHours(3)))
            .WithMemoryCache((sp, opts) =>
            {
                opts.DefaultExpiration = sp.GetRequiredService<IOptions<BuilderSiblingOptions>>().Value.CacheExpiration;
                invokedWithExpiration = opts.DefaultExpiration;
            })
            .Build();

        Assert.NotNull(client);
        Assert.NotNull(GetCacheManager(client));
        Assert.Equal(TimeSpan.FromHours(3), invokedWithExpiration);
    }

    [Fact]
    public async Task Build_WithHybridCache_ServiceProviderCallback_InvokesCallbackOnResolution()
    {
        var distributedCache = new TestDistributedCache();
        var invokedWithExpiration = TimeSpan.Zero;

        await using var client = DeliveryClientBuilder
            .WithOptions(builder => builder
                .WithEnvironmentId(EnvironmentId)
                .UseProductionApi()
                .Build())
            .ConfigureServices(services => services.Configure<BuilderSiblingOptions>(o => o.CacheExpiration = TimeSpan.FromHours(2)))
            .WithHybridCache(distributedCache, (sp, opts) =>
            {
                opts.DefaultExpiration = sp.GetRequiredService<IOptions<BuilderSiblingOptions>>().Value.CacheExpiration;
                invokedWithExpiration = opts.DefaultExpiration;
            })
            .Build();

        Assert.NotNull(client);
        Assert.NotNull(GetCacheManager(client));
        Assert.Equal(TimeSpan.FromHours(2), invokedWithExpiration);
    }

    [Fact]
    public void WithMemoryCache_ServiceProviderCallback_Null_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            DeliveryClientBuilder
                .WithOptions(builder => builder
                    .WithEnvironmentId(EnvironmentId)
                    .UseProductionApi()
                    .Build())
                .WithMemoryCache((Action<IServiceProvider, DeliveryCacheOptions>)null!));
    }

    [Fact]
    public void WithHybridCache_ServiceProviderCallback_NullDistributedCache_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            DeliveryClientBuilder
                .WithOptions(builder => builder
                    .WithEnvironmentId(EnvironmentId)
                    .UseProductionApi()
                    .Build())
                .WithHybridCache(null!, (Action<IServiceProvider, DeliveryCacheOptions>)((_, o) => o.DefaultExpiration = TimeSpan.FromMinutes(30))));
    }

    [Fact]
    public void WithHybridCache_ServiceProviderCallback_NullDelegate_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            DeliveryClientBuilder
                .WithOptions(builder => builder
                    .WithEnvironmentId(EnvironmentId)
                    .UseProductionApi()
                    .Build())
                .WithHybridCache(new TestDistributedCache(), (Action<IServiceProvider, DeliveryCacheOptions>)null!));
    }

    // Simple test implementation of ITypeProvider
    private class TestTypeProvider : ITypeProvider
    {
        public Type? GetType(string contentType) => null;
        public string? GetCodename(Type contentType) => null;
    }

    private static IDeliveryCacheManager? GetCacheManager(IDeliveryClient client)
    {
        // Unwrap OwnedDeliveryClient to get the inner DeliveryClient
        var target = client;
        var innerField = client.GetType()
            .GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
            .FirstOrDefault(f => typeof(IDeliveryClient).IsAssignableFrom(f.FieldType));
        if (innerField?.GetValue(client) is IDeliveryClient inner)
            target = inner;

        var cacheManagerField = target.GetType()
            .GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
            .FirstOrDefault(f => typeof(IDeliveryCacheManager).IsAssignableFrom(f.FieldType));
        return cacheManagerField?.GetValue(target) as IDeliveryCacheManager;
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
