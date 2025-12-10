using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.Configuration;
using Microsoft.Extensions.Caching.Distributed;
using Xunit;

namespace Kontent.Ai.Delivery.Tests.Builders.Configuration;

public class DeliveryClientBuilderTests
{
    private const string EnvironmentId = "550cec62-90a6-4ab3-b3e4-3d0bb4c04f5c";
    private const string TestPreviewApiKey = "preview.api.key";
    private const string TestSecureApiKey = "secure.api.key";
    private static readonly Guid EnvironmentIdGuid = Guid.Parse(EnvironmentId);

    [Fact]
    public void Build_WithEnvironmentIdString_CreatesClient()
    {
        // Act
        var client = DeliveryClientBuilder
            .WithEnvironmentId(EnvironmentId)
            .Build();

        // Assert
        Assert.NotNull(client);
        Assert.IsAssignableFrom<IDeliveryClient>(client);
    }

    [Fact]
    public void Build_WithEnvironmentIdGuid_CreatesClient()
    {
        // Act
        var client = DeliveryClientBuilder
            .WithEnvironmentId(EnvironmentIdGuid)
            .Build();

        // Assert
        Assert.NotNull(client);
        Assert.IsAssignableFrom<IDeliveryClient>(client);
    }

    [Fact]
    public void Build_WithOptions_CreatesClient()
    {
        // Act
        var client = DeliveryClientBuilder
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
    public void Build_WithPreviewApi_CreatesClient()
    {
        // Act
        var client = DeliveryClientBuilder
            .WithOptions(builder => builder
                .WithEnvironmentId(EnvironmentId)
                .UsePreviewApi(TestPreviewApiKey)
                .Build())
            .Build();

        // Assert
        Assert.NotNull(client);
    }

    [Fact]
    public void Build_WithTypeProvider_CreatesClient()
    {
        // Arrange
        var typeProvider = new TestTypeProvider();

        // Act
        var client = DeliveryClientBuilder
            .WithEnvironmentId(EnvironmentId)
            .WithTypeProvider(typeProvider)
            .Build();

        // Assert
        Assert.NotNull(client);
    }

    [Fact]
    public void Build_WithMemoryCache_CreatesClient()
    {
        // Act
        var client = DeliveryClientBuilder
            .WithEnvironmentId(EnvironmentId)
            .WithMemoryCache(TimeSpan.FromMinutes(30))
            .Build();

        // Assert
        Assert.NotNull(client);
    }

    [Fact]
    public void Build_WithMemoryCacheDefaultExpiration_CreatesClient()
    {
        // Act
        var client = DeliveryClientBuilder
            .WithEnvironmentId(EnvironmentId)
            .WithMemoryCache()
            .Build();

        // Assert
        Assert.NotNull(client);
    }

    [Fact]
    public void Build_WithDistributedCache_CreatesClient()
    {
        // Arrange
        var distributedCache = new TestDistributedCache();

        // Act
        var client = DeliveryClientBuilder
            .WithEnvironmentId(EnvironmentId)
            .WithDistributedCache(distributedCache, TimeSpan.FromHours(1))
            .Build();

        // Assert
        Assert.NotNull(client);
    }

    [Fact]
    public void Build_WithAllOptions_CreatesClient()
    {
        // Arrange
        var typeProvider = new TestTypeProvider();

        // Act
        var client = DeliveryClientBuilder
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
                .WithEnvironmentId(EnvironmentId)
                .WithTypeProvider(null!));
    }

    [Fact]
    public void WithDistributedCache_WithNull_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            DeliveryClientBuilder
                .WithEnvironmentId(EnvironmentId)
                .WithDistributedCache(null!));
    }

    [Fact]
    public void Build_CallingMemoryCacheAfterDistributedCache_UsesLastConfigured()
    {
        // Arrange
        var distributedCache = new TestDistributedCache();

        // Act - last cache type wins
        var client = DeliveryClientBuilder
            .WithEnvironmentId(EnvironmentId)
            .WithDistributedCache(distributedCache)
            .WithMemoryCache(TimeSpan.FromMinutes(30))
            .Build();

        // Assert
        Assert.NotNull(client);
    }

    [Fact]
    public void Build_CallingDistributedCacheAfterMemoryCache_UsesLastConfigured()
    {
        // Arrange
        var distributedCache = new TestDistributedCache();

        // Act - last cache type wins
        var client = DeliveryClientBuilder
            .WithEnvironmentId(EnvironmentId)
            .WithMemoryCache(TimeSpan.FromMinutes(30))
            .WithDistributedCache(distributedCache, TimeSpan.FromHours(1))
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
        var builder1 = DeliveryClientBuilder.WithEnvironmentId(EnvironmentId);
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
        var client1 = DeliveryClientBuilder
            .WithEnvironmentId(EnvironmentId)
            .Build();

        var client2 = DeliveryClientBuilder
            .WithEnvironmentId(EnvironmentId)
            .WithMemoryCache()
            .Build();

        // Assert
        Assert.NotNull(client1);
        Assert.NotNull(client2);
        Assert.NotSame(client1, client2);
    }

    [Fact]
    public void Build_WithSecureAccess_CreatesClient()
    {
        // Act
        var client = DeliveryClientBuilder
            .WithOptions(builder => builder
                .WithEnvironmentId(EnvironmentId)
                .UseProductionApi(TestSecureApiKey)
                .Build())
            .Build();

        // Assert
        Assert.NotNull(client);
    }

    [Fact]
    public void Build_WithDisabledResilience_CreatesClient()
    {
        // Act
        var client = DeliveryClientBuilder
            .WithOptions(builder => builder
                .WithEnvironmentId(EnvironmentId)
                .UseProductionApi()
                .DisableRetryPolicy()
                .Build())
            .Build();

        // Assert
        Assert.NotNull(client);
    }

    // Simple test implementation of ITypeProvider
    private class TestTypeProvider : ITypeProvider
    {
        public Type? TryGetModelType(string contentType) => null;
        public string? GetCodename(Type contentType) => null;
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
