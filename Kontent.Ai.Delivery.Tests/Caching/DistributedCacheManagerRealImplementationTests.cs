using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.Caching;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;

namespace Kontent.Ai.Delivery.Tests.Caching;

/// <summary>
/// Integration tests for DistributedCacheManager using Microsoft's official AddDistributedMemoryCache implementation.
/// These tests verify that our cache manager works correctly with a real IDistributedCache implementation,
/// not just our custom mock.
/// </summary>
public class DistributedCacheManagerRealImplementationTests
{
    private readonly DistributedCacheManager _cacheManager;

    public DistributedCacheManagerRealImplementationTests()
    {
        // Use Microsoft's official in-memory distributed cache implementation
        var services = new ServiceCollection();
        services.AddDistributedMemoryCache();
        var serviceProvider = services.BuildServiceProvider();

        var distributedCache = serviceProvider.GetRequiredService<IDistributedCache>();
        _cacheManager = new DistributedCacheManager(distributedCache, new DeliveryCacheOptions { DefaultExpiration = TimeSpan.FromMinutes(5) });
    }

    #region Basic Operations

    [Fact]
    public async Task GetOrSetAsync_CacheMissAndHit_WithRealImplementation()
    {
        var key = "real_test_key";
        var value = new TestValue { Id = 42, Name = "Integration Test" };

        // First call: cache miss, factory is called
        var factoryCalled = false;
        var result = await _cacheManager.GetOrSetAsync(key, _ =>
        {
            factoryCalled = true;
            return Task.FromResult<CacheEntry<TestValue>?>(
                new CacheEntry<TestValue>(value, ["dep1"]));
        });

        Assert.True(factoryCalled);
        Assert.NotNull(result);
        Assert.Equal(value.Id, result.Id);
        Assert.Equal(value.Name, result.Name);

        // Second call: cache hit, factory is NOT called
        factoryCalled = false;
        var cached = await _cacheManager.GetOrSetAsync(key, _ =>
        {
            factoryCalled = true;
            return Task.FromResult<CacheEntry<TestValue>?>(null);
        });

        Assert.False(factoryCalled);
        Assert.NotNull(cached);
        Assert.Equal(value.Id, cached.Id);
    }

    [Fact]
    public async Task GetOrSetAsync_FactoryReturnsNull_WithRealImplementation()
    {
        var result = await _cacheManager.GetOrSetAsync<TestValue>("non_existent_key_real", _ =>
            Task.FromResult<CacheEntry<TestValue>?>(null));

        Assert.Null(result);
    }

    #endregion

    #region Dependency Tracking and Invalidation

    [Fact]
    public async Task InvalidateAsync_WithRealImplementation_RemovesCacheEntry()
    {
        var dependency = "invalidate_dep";
        await PopulateCache("invalidate_test_key", new TestValue { Id = 100, Name = "To Be Invalidated" }, [dependency]);

        await _cacheManager.InvalidateAsync(default, dependency);

        Assert.True(await IsFactoryCalledAsync("invalidate_test_key"));
    }

    [Fact]
    public async Task InvalidateAsync_SharedDependency_WithRealImplementation_RemovesAllEntries()
    {
        var value = new TestValue { Id = 1, Name = "Test" };
        var sharedDependency = "shared_real_dep";

        await PopulateCache("shared_key1", value, [sharedDependency]);
        await PopulateCache("shared_key2", value, [sharedDependency]);
        await PopulateCache("other_key", value, ["other_dep_real"]);

        await _cacheManager.InvalidateAsync(default, sharedDependency);

        Assert.True(await IsFactoryCalledAsync("shared_key1"));
        Assert.True(await IsFactoryCalledAsync("shared_key2"));
        Assert.False(await IsFactoryCalledAsync("other_key"));
    }

    [Fact]
    public async Task InvalidateAsync_MultipleDependencies_WithRealImplementation_RemovesCorrectEntries()
    {
        var value = new TestValue { Id = 1, Name = "Test" };

        await PopulateCache("multi_key1", value, ["multi_dep1"]);
        await PopulateCache("multi_key2", value, ["multi_dep2"]);
        await PopulateCache("multi_key3", value, ["multi_dep3"]);

        await _cacheManager.InvalidateAsync(default, "multi_dep1", "multi_dep2");

        Assert.True(await IsFactoryCalledAsync("multi_key1"));
        Assert.True(await IsFactoryCalledAsync("multi_key2"));
        Assert.False(await IsFactoryCalledAsync("multi_key3"));
    }

    #endregion

    #region Complex Object Serialization

    [Fact]
    public async Task GetOrSetAsync_ComplexObject_WithRealImplementation_SerializesCorrectly()
    {
        var value = new ComplexValue
        {
            Id = 1,
            Name = "Complex Object",
            Nested = new NestedValue
            {
                Description = "Nested description",
                Tags = ["tag1", "tag2", "tag3"]
            },
            Items = [10, 20, 30]
        };

        await _cacheManager.GetOrSetAsync("complex_real_key", _ =>
            Task.FromResult<CacheEntry<ComplexValue>?>(
                new CacheEntry<ComplexValue>(value, [])));

        var factoryCalled = false;
        var result = await _cacheManager.GetOrSetAsync("complex_real_key", _ =>
        {
            factoryCalled = true;
            return Task.FromResult<CacheEntry<ComplexValue>?>(null);
        });

        Assert.False(factoryCalled);
        Assert.NotNull(result);
        Assert.Equal(value.Id, result.Id);
        Assert.Equal(value.Name, result.Name);
        Assert.NotNull(result.Nested);
        Assert.Equal(value.Nested.Description, result.Nested.Description);
        Assert.Equal(value.Nested.Tags, result.Nested.Tags);
        Assert.Equal(value.Items, result.Items);
    }

    [Fact]
    public async Task GetOrSetAsync_ObjectWithNullProperties_WithRealImplementation_HandlesCorrectly()
    {
        var value = new TestValue { Id = 99, Name = null };

        await PopulateCache("null_props_real_key", value, []);

        var factoryCalled = false;
        var result = await _cacheManager.GetOrSetAsync("null_props_real_key", _ =>
        {
            factoryCalled = true;
            return Task.FromResult<CacheEntry<TestValue>?>(null);
        });

        Assert.False(factoryCalled);
        Assert.NotNull(result);
        Assert.Equal(value.Id, result.Id);
        Assert.Null(result.Name);
    }

    [Fact]
    public async Task GetOrSetAsync_ObjectWithCircularReference_WithRealImplementation_ThrowsSerializationException()
    {
        var value = new CircularValue { Id = 1, Name = "Parent" };
        value.Self = value; // Circular reference

        // Distributed cache requires serialization, so circular references cause an error.
        var exception = await Record.ExceptionAsync(() =>
            _cacheManager.GetOrSetAsync("circular_real_key", _ =>
                Task.FromResult<CacheEntry<CircularValue>?>(
                    new CacheEntry<CircularValue>(value, []))));
        Assert.NotNull(exception);
    }

    #endregion

    #region Concurrent Operations

    [Fact]
    public async Task ConcurrentGetOrSet_WithRealImplementation_HandlesGracefully()
    {
        var tasks = Enumerable.Range(0, 20)
            .Select(i => PopulateCache(
                $"concurrent_real_key_{i}",
                new TestValue { Id = i, Name = $"Test_{i}" },
                [$"concurrent_real_dep_{i}"]))
            .ToArray();

        await Task.WhenAll(tasks);

        var verifyTasks = Enumerable.Range(0, 20)
            .Select(i => IsFactoryCalledAsync($"concurrent_real_key_{i}"))
            .ToArray();

        var results = await Task.WhenAll(verifyTasks);
        Assert.All(results, r => Assert.False(r));
    }

    [Fact]
    public async Task ConcurrentGetOrSet_SameKey_WithRealImplementation_ReturnsConsistentResults()
    {
        var value = new TestValue { Id = 42, Name = "Concurrent Test" };
        await PopulateCache("concurrent_get_real_key", value, []);

        var tasks = Enumerable.Range(0, 50)
            .Select(_ => _cacheManager.GetOrSetAsync("concurrent_get_real_key", _ =>
                Task.FromResult<CacheEntry<TestValue>?>(
                    new CacheEntry<TestValue>(new TestValue { Id = 99 }, []))))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        Assert.All(results, r =>
        {
            Assert.NotNull(r);
            Assert.Equal(value.Id, r.Id);
        });
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task GetOrSetAsync_VeryLongKey_WithRealImplementation_Succeeds()
    {
        var key = new string('x', 500);
        var value = new TestValue { Id = 1, Name = "Long Key Test" };

        await PopulateCache(key, value, []);
        Assert.False(await IsFactoryCalledAsync(key));
    }

    [Fact]
    public async Task GetOrSetAsync_ManyDependencies_WithRealImplementation_TracksAllDependencies()
    {
        var value = new TestValue { Id = 1, Name = "Test" };
        var dependencies = Enumerable.Range(0, 50).Select(i => $"real_dep_{i}").ToArray();

        await _cacheManager.GetOrSetAsync("many_deps_real_key", _ =>
            Task.FromResult<CacheEntry<TestValue>?>(
                new CacheEntry<TestValue>(value, dependencies)));

        await _cacheManager.InvalidateAsync(default, "real_dep_25");
        Assert.True(await IsFactoryCalledAsync("many_deps_real_key"));
    }

    [Fact]
    public async Task InvalidateAsync_NonExistentDependency_WithRealImplementation_DoesNotThrowAndPreservesExistingEntries()
    {
        await PopulateCache("existing_real_key", new TestValue { Id = 77, Name = "Existing" }, ["existing_real_dep"]);

        var exception = await Record.ExceptionAsync(() =>
            _cacheManager.InvalidateAsync(default, "non_existent_real_dep"));

        Assert.Null(exception);
        Assert.False(await IsFactoryCalledAsync("existing_real_key"));
    }

    #endregion

    #region Test Helpers

    private Task PopulateCache(string key, TestValue value, string[] dependencies)
        => _cacheManager.GetOrSetAsync(key, _ =>
            Task.FromResult<CacheEntry<TestValue>?>(
                new CacheEntry<TestValue>(value, dependencies)));

    private async Task<bool> IsFactoryCalledAsync(string key)
    {
        var factoryCalled = false;
        await _cacheManager.GetOrSetAsync<TestValue>(key, _ =>
        {
            factoryCalled = true;
            return Task.FromResult<CacheEntry<TestValue>?>(null);
        });
        return factoryCalled;
    }

    #endregion

    #region Test Helper Classes

    private class TestValue
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }

    private class ComplexValue
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public NestedValue? Nested { get; set; }
        public int[]? Items { get; set; }
    }

    private class NestedValue
    {
        public string? Description { get; set; }
        public string[]? Tags { get; set; }
    }

    private class CircularValue
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public CircularValue? Self { get; set; }
    }

    #endregion
}
