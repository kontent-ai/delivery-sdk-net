using System.Diagnostics;
using System.Text.Json;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.Caching;
using Kontent.Ai.Delivery.Configuration;
using Kontent.Ai.Delivery.ContentTypes;
using Microsoft.Extensions.Caching.Distributed;

namespace Kontent.Ai.Delivery.Tests.Caching;

/// <summary>
/// Comprehensive tests for HybridCacheManager (hybrid L1+L2 cache) implementation.
/// Tests cover: basic operations, dependency tracking, invalidation, serialization, concurrency, and error handling.
/// </summary>
public class HybridCacheManagerTests
{
    private readonly MockDistributedCache _mockCache;
    private readonly HybridCacheManager _cacheManager;

    public HybridCacheManagerTests()
    {
        _mockCache = new MockDistributedCache();
        _cacheManager = new HybridCacheManager(_mockCache, new DeliveryCacheOptions { DefaultExpiration = TimeSpan.FromMinutes(5) });
    }

    #region Basic Operations Tests

    [Fact]
    public async Task GetOrSetAsync_CacheMiss_CallsFactory()
    {
        var value = new TestCacheValue { Id = 1, Name = "Test" };
        var factoryCalled = false;

        var result = await _cacheManager.GetOrSetAsync("test_key", _ =>
        {
            factoryCalled = true;
            return Task.FromResult<CacheEntry<TestCacheValue>?>(
                new CacheEntry<TestCacheValue>(value, ["dep1"]));
        });

        Assert.True(factoryCalled);
        Assert.NotNull(result);
        Assert.Equal(value.Id, result.Value.Id);
        Assert.Equal(value.Name, result.Value.Name);
    }

    [Fact]
    public async Task GetOrSetAsync_CacheHit_DoesNotCallFactory()
    {
        var value = new TestCacheValue { Id = 1, Name = "Test" };
        await PopulateCache("test_key", value, ["dep1"]);

        var factoryCalled = false;
        var result = await _cacheManager.GetOrSetAsync("test_key", _ =>
        {
            factoryCalled = true;
            return Task.FromResult<CacheEntry<TestCacheValue>?>(null);
        });

        Assert.False(factoryCalled);
        Assert.NotNull(result);
        Assert.Equal(value.Id, result.Value.Id);
        Assert.Equal(value.Name, result.Value.Name);
    }

    [Fact]
    public async Task GetOrSetAsync_FactoryReturnsNull_ReturnsNull()
    {
        var result = await _cacheManager.GetOrSetAsync<TestCacheValue>("null_factory_key", _ =>
            Task.FromResult<CacheEntry<TestCacheValue>?>(null));

        Assert.Null(result);
    }

    [Fact]
    public async Task GetOrSetAsync_OverwritesCachedValue_OnNextMiss()
    {
        var value1 = new TestCacheValue { Id = 1, Name = "First" };
        await PopulateCache("test_key", value1, []);

        await _cacheManager.InvalidateAsync(default, "some_dep");

        // After invalidating a non-matching dep, original should still be cached
        Assert.False(await IsFactoryCalledAsync("test_key"));
    }

    [Fact]
    public async Task GetOrSetAsync_EmptyDependencies_DoesNotThrow()
    {
        var value = new TestCacheValue { Id = 1, Name = "Test" };

        await PopulateCache("test_key", value, []);

        Assert.False(await IsFactoryCalledAsync("test_key"));
    }

    #endregion

    #region Expiration Tests

    [Fact]
    public void Constructor_WithDefaultExpiration_AcceptsValue()
    {
        var expiration = TimeSpan.FromMinutes(30);
        var manager = new HybridCacheManager(_mockCache, new DeliveryCacheOptions { DefaultExpiration = expiration });

        Assert.NotNull(manager);
    }

    [Fact]
    public void Constructor_WithNullExpiration_UsesDefaultOneHour() =>
        Assert.NotNull(new HybridCacheManager(_mockCache, new DeliveryCacheOptions()));

    [Fact]
    public async Task GetOrSetAsync_WithCustomExpiration_DoesNotThrow()
    {
        var value = new TestCacheValue { Id = 1, Name = "Test" };

        var result = await _cacheManager.GetOrSetAsync("test_key", _ =>
            Task.FromResult<CacheEntry<TestCacheValue>?>(
                new CacheEntry<TestCacheValue>(value, [])),
            expiration: TimeSpan.FromMinutes(15));

        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetOrSetAsync_ExpirationPassedToCacheEntry()
    {
        var manager = new HybridCacheManager(_mockCache, new DeliveryCacheOptions { DefaultExpiration = TimeSpan.FromHours(2) });
        var value = new TestCacheValue { Id = 1, Name = "Test" };

        await manager.GetOrSetAsync("test_key", _ =>
            Task.FromResult<CacheEntry<TestCacheValue>?>(
                new CacheEntry<TestCacheValue>(value, [])),
            expiration: TimeSpan.FromMilliseconds(80));

        var expired = await WaitUntilAsync(
            () => IsFactoryCalledAsync("test_key", manager),
            timeout: TimeSpan.FromSeconds(2),
            pollInterval: TimeSpan.FromMilliseconds(20));

        Assert.True(expired);
    }

    [Fact]
    public async Task GetOrSetAsync_WithoutCustomExpiration_UsesDefaultExpiration()
    {
        var defaultExpiration = TimeSpan.FromMilliseconds(80);
        var manager = new HybridCacheManager(_mockCache, new DeliveryCacheOptions { DefaultExpiration = defaultExpiration });
        var value = new TestCacheValue { Id = 1, Name = "Test" };

        await manager.GetOrSetAsync("test_key", _ =>
            Task.FromResult<CacheEntry<TestCacheValue>?>(
                new CacheEntry<TestCacheValue>(value, [])));

        var expired = await WaitUntilAsync(
            () => IsFactoryCalledAsync("test_key", manager),
            timeout: TimeSpan.FromSeconds(2),
            pollInterval: TimeSpan.FromMilliseconds(20));

        Assert.True(expired);
    }

    #endregion

    #region Serialization Tests

    [Fact]
    public async Task GetOrSetAsync_SimpleObject_SerializesCorrectly()
    {
        var value = new TestCacheValue { Id = 42, Name = "Test Value" };

        await PopulateCache("test_key", value, []);
        var result = await GetCachedValue<TestCacheValue>("test_key");

        Assert.NotNull(result);
        Assert.Equal(value.Id, result.Id);
        Assert.Equal(value.Name, result.Name);
    }

    [Fact]
    public async Task GetOrSetAsync_ComplexObject_SerializesCorrectly()
    {
        var value = new ComplexCacheValue
        {
            Id = 1,
            Name = "Complex",
            Nested = new NestedValue
            {
                Description = "Nested description",
                Tags = ["tag1", "tag2", "tag3"]
            },
            Items = [1, 2, 3, 4, 5]
        };

        await _cacheManager.GetOrSetAsync("complex_key", _ =>
            Task.FromResult<CacheEntry<ComplexCacheValue>?>(
                new CacheEntry<ComplexCacheValue>(value, [])));

        var factoryCalled = false;
        var result = await _cacheManager.GetOrSetAsync("complex_key", _ =>
        {
            factoryCalled = true;
            return Task.FromResult<CacheEntry<ComplexCacheValue>?>(null);
        });

        Assert.False(factoryCalled);
        Assert.NotNull(result);
        Assert.Equal(value.Id, result.Value.Id);
        Assert.Equal(value.Name, result.Value.Name);
        Assert.NotNull(result.Value.Nested);
        Assert.Equal(value.Nested.Description, result.Value.Nested.Description);
        Assert.Equal(value.Nested.Tags, result.Value.Nested.Tags);
        Assert.Equal(value.Items, result.Value.Items);
    }

    [Fact]
    public async Task GetOrSetAsync_ObjectWithNullProperties_SerializesCorrectly()
    {
        var value = new TestCacheValue { Id = 1, Name = null };

        await PopulateCache("null_props_key", value, []);

        var factoryCalled = false;
        var result = await _cacheManager.GetOrSetAsync("null_props_key", _ =>
        {
            factoryCalled = true;
            return Task.FromResult<CacheEntry<TestCacheValue>?>(null);
        });

        Assert.False(factoryCalled);
        Assert.NotNull(result);
        Assert.Equal(value.Id, result.Value.Id);
        Assert.Null(result.Value.Name);
    }

    [Fact]
    public async Task GetOrSetAsync_ObjectWithCircularReference_ThrowsSerializationException()
    {
        var value = new CircularReferenceValue { Id = 1, Name = "Parent" };
        value.Self = value;

        // Distributed cache requires serialization, so circular references cause an error.
        var exception = await Record.ExceptionAsync(() =>
            _cacheManager.GetOrSetAsync("circular_key", _ =>
                Task.FromResult<CacheEntry<CircularReferenceValue>?>(
                    new CacheEntry<CircularReferenceValue>(value, []))));
        Assert.NotNull(exception);
    }

    [Fact]
    public async Task GetOrSetAsync_ContentTypeFromApiDeserializer_CanRoundTripFromCache()
    {
        var fixturePath = Path.Combine(
            Environment.CurrentDirectory,
            "Fixtures",
            "DeliveryClient",
            "article.json");
        var fixtureJson = await File.ReadAllTextAsync(fixturePath);
        var apiJsonOptions = RefitSettingsProvider.CreateDefaultJsonSerializerOptions();
        var contentType = JsonSerializer.Deserialize<ContentType>(fixtureJson, apiJsonOptions);

        Assert.NotNull(contentType);

        await _cacheManager.GetOrSetAsync("content-type-key", _ =>
            Task.FromResult<CacheEntry<ContentType>?>(
                new CacheEntry<ContentType>(contentType, [])));

        var factoryCalled = false;
        var cached = await _cacheManager.GetOrSetAsync("content-type-key", _ =>
        {
            factoryCalled = true;
            return Task.FromResult<CacheEntry<ContentType>?>(null);
        });

        Assert.False(factoryCalled);
        Assert.NotNull(cached);
        Assert.Equal(contentType.System.Codename, cached.Value.System.Codename);
    }

    #endregion

    #region Dependency Tracking Tests

    [Fact]
    public async Task GetOrSetAsync_WithDependencies_InvalidateRemovesEntry()
    {
        var value = new TestCacheValue { Id = 1, Name = "Test" };
        await PopulateCache("test_key", value, ["dep1", "dep2"]);

        await _cacheManager.InvalidateAsync(default, "dep1");

        Assert.True(await IsFactoryCalledAsync("test_key"));
    }

    [Fact]
    public async Task GetOrSetAsync_SameDependencyWithShorterTtl_StillInvalidatesAllEntries()
    {
        var manager = new HybridCacheManager(_mockCache, new DeliveryCacheOptions { DefaultExpiration = TimeSpan.FromMinutes(5) });
        var dependency = "dep_shared";

        await manager.GetOrSetAsync("long_ttl_key", _ =>
            Task.FromResult<CacheEntry<TestCacheValue>?>(
                new CacheEntry<TestCacheValue>(new TestCacheValue { Id = 1, Name = "Long" }, [dependency])),
            expiration: TimeSpan.FromMinutes(30));

        await manager.GetOrSetAsync("short_ttl_key", _ =>
            Task.FromResult<CacheEntry<TestCacheValue>?>(
                new CacheEntry<TestCacheValue>(new TestCacheValue { Id = 2, Name = "Short" }, [dependency])),
            expiration: TimeSpan.FromMinutes(1));

        await manager.InvalidateAsync(default, dependency);

        Assert.True(await IsFactoryCalledAsync("long_ttl_key", manager));
        Assert.True(await IsFactoryCalledAsync("short_ttl_key", manager));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetOrSetAsync_WithIgnoredDependencyValue_IgnoresInvalidDependency(string? ignoredDependency)
    {
        var value = new TestCacheValue { Id = 1, Name = "Test" };

        await _cacheManager.GetOrSetAsync("test_key", _ =>
            Task.FromResult<CacheEntry<TestCacheValue>?>(
                new CacheEntry<TestCacheValue>(value, ["dep1", ignoredDependency!, "dep2"])));

        Assert.False(await IsFactoryCalledAsync("test_key"));
    }

    #endregion

    #region Invalidation Tests

    [Fact]
    public async Task InvalidateAsync_RemovesCacheEntry_ReturnsTrue()
    {
        var dependency = "dep1";
        await PopulateCache("test_key", new TestCacheValue { Id = 1, Name = "Test" }, [dependency]);

        var result = await _cacheManager.InvalidateAsync(default, dependency);

        Assert.True(result);
        Assert.True(await IsFactoryCalledAsync("test_key"));
    }

    [Fact]
    public async Task InvalidateAsync_NonExistentDependency_ReturnsTrueAndPreservesExistingEntries()
    {
        await PopulateCache("existing_key", new TestCacheValue { Id = 10, Name = "Existing" }, ["existing_dep"]);

        var result = await _cacheManager.InvalidateAsync(default, "non_existent_dep");

        Assert.True(result);
        Assert.False(await IsFactoryCalledAsync("existing_key"));
    }

    [Fact]
    public async Task InvalidateAsync_NullDependencies_ReturnsTrue()
    {
        var result = await _cacheManager.InvalidateAsync(default, null!);
        Assert.True(result);
    }

    [Fact]
    public async Task InvalidateAsync_EmptyDependencies_ReturnsTrue()
    {
        var result = await _cacheManager.InvalidateAsync(default, []);
        Assert.True(result);
    }

    [Fact]
    public async Task InvalidateAsync_MultipleDependencies_RemovesAllAffected()
    {
        var value = new TestCacheValue { Id = 1, Name = "Test" };
        await PopulateCache("key1", value, ["dep1"]);
        await PopulateCache("key2", value, ["dep2"]);
        await PopulateCache("key3", value, ["dep3"]);

        await _cacheManager.InvalidateAsync(default, "dep1", "dep2");

        Assert.True(await IsFactoryCalledAsync("key1"));
        Assert.True(await IsFactoryCalledAsync("key2"));
        Assert.False(await IsFactoryCalledAsync("key3"));
    }

    [Fact]
    public async Task InvalidateAsync_SharedDependency_RemovesAllEntriesWithThatDependency()
    {
        var value = new TestCacheValue { Id = 1, Name = "Test" };
        var sharedDependency = "shared_dep";

        await PopulateCache("key1", value, [sharedDependency]);
        await PopulateCache("key2", value, [sharedDependency]);
        await PopulateCache("key3", value, ["other_dep"]);

        await _cacheManager.InvalidateAsync(default, sharedDependency);

        Assert.True(await IsFactoryCalledAsync("key1"));
        Assert.True(await IsFactoryCalledAsync("key2"));
        Assert.False(await IsFactoryCalledAsync("key3"));
    }

    [Fact]
    public async Task InvalidateAsync_IdempotentOperation_CanBeCalledMultipleTimes()
    {
        var dependency = "dep1";
        await PopulateCache("test_key", new TestCacheValue { Id = 1, Name = "Test" }, [dependency]);

        await _cacheManager.InvalidateAsync(default, dependency);
        await _cacheManager.InvalidateAsync(default, dependency);
        await _cacheManager.InvalidateAsync(default, dependency);

        Assert.True(await IsFactoryCalledAsync("test_key"));
    }

    #endregion

    #region Concurrency Tests

    [Fact]
    public async Task ConcurrentGetOrSet_SameKey_ReturnsConsistentResults()
    {
        var value = new TestCacheValue { Id = 1, Name = "Test" };
        await PopulateCache("test_key", value, []);

        var tasks = Enumerable.Range(0, 100)
            .Select(_ => _cacheManager.GetOrSetAsync("test_key", _ =>
                Task.FromResult<CacheEntry<TestCacheValue>?>(
                    new CacheEntry<TestCacheValue>(new TestCacheValue { Id = 99 }, []))))
            .ToList();

        var results = await Task.WhenAll(tasks);
        Assert.All(results, r =>
        {
            Assert.NotNull(r);
            Assert.Equal(value.Id, r.Value.Id);
        });
    }

    [Fact]
    public async Task ConcurrentPopulate_WithSameDependency_ThenInvalidate()
    {
        var sharedDependency = "shared_dep";
        var tasks = Enumerable.Range(0, 50)
            .Select(i => PopulateCache(
                $"key_{i}",
                new TestCacheValue { Id = i, Name = $"Test_{i}" },
                [sharedDependency]))
            .ToList();

        await Task.WhenAll(tasks);

        await _cacheManager.InvalidateAsync(default, sharedDependency);

        var verifyTasks = Enumerable.Range(0, 50)
            .Select(i => IsFactoryCalledAsync($"key_{i}"))
            .ToList();

        var results = await Task.WhenAll(verifyTasks);
        Assert.All(results, Assert.True);
    }

    [Fact]
    public async Task ConcurrentInvalidate_SameDependency_DoesNotThrow()
    {
        var dependency = "dep1";
        await PopulateCache("test_key", new TestCacheValue { Id = 1, Name = "Test" }, [dependency]);

        var tasks = Enumerable.Range(0, 50)
            .Select(_ => _cacheManager.InvalidateAsync(default, dependency))
            .ToList();

        await Task.WhenAll(tasks);

        Assert.True(await IsFactoryCalledAsync("test_key"));
    }

    #endregion

    #region Cancellation Tests

    [Fact]
    public async Task GetOrSetAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _cacheManager.GetOrSetAsync<TestCacheValue>("key", _ =>
                Task.FromResult<CacheEntry<TestCacheValue>?>(null), cancellationToken: cts.Token));
    }

    [Fact]
    public async Task InvalidateAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _cacheManager.InvalidateAsync(cts.Token, "dep1"));
    }

    #endregion

    #region Cache Key Prefix Tests

    [Fact]
    public async Task GetOrSetAsync_WithPrefixedManager_DoesNotLeakToDefaultNamespace()
    {
        var value = new TestCacheValue { Id = 1, Name = "Test" };
        var prefixedManager = new HybridCacheManager(_mockCache, new DeliveryCacheOptions { KeyPrefix = "prefixed" });

        await PopulateCache("test_key", value, [], prefixedManager);

        Assert.False(await IsFactoryCalledAsync("test_key", prefixedManager));
        Assert.True(await IsFactoryCalledAsync("test_key", _cacheManager));
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task GetOrSetAsync_VeryLongKey_Succeeds()
    {
        var key = new string('a', 1000);
        var value = new TestCacheValue { Id = 1, Name = "Test" };

        await PopulateCache(key, value, []);
        Assert.False(await IsFactoryCalledAsync(key));
    }

    [Fact]
    public async Task GetOrSetAsync_VeryLongDependency_Succeeds()
    {
        var dependency = new string('b', 1000);
        var value = new TestCacheValue { Id = 1, Name = "Test" };

        await PopulateCache("test_key", value, [dependency]);
        await _cacheManager.InvalidateAsync(default, dependency);

        Assert.True(await IsFactoryCalledAsync("test_key"));
    }

    [Fact]
    public async Task GetOrSetAsync_ManyDependencies_Succeeds()
    {
        var value = new TestCacheValue { Id = 1, Name = "Test" };
        var dependencies = Enumerable.Range(0, 100).Select(i => $"dep_{i}").ToArray();

        await _cacheManager.GetOrSetAsync("test_key", _ =>
            Task.FromResult<CacheEntry<TestCacheValue>?>(
                new CacheEntry<TestCacheValue>(value, dependencies)));

        Assert.False(await IsFactoryCalledAsync("test_key"));
    }

    [Fact]
    public async Task InvalidateAsync_ManyDependencies_Succeeds()
    {
        var value = new TestCacheValue { Id = 1, Name = "Test" };

        foreach (var i in Enumerable.Range(0, 50))
        {
            await PopulateCache($"key_{i}", value, [$"dep_key_{i}"]);
        }

        var dependenciesToInvalidate = Enumerable.Range(0, 50).Select(i => $"dep_key_{i}").ToArray();
        await _cacheManager.InvalidateAsync(default, dependenciesToInvalidate);

        var results = await Task.WhenAll(
            Enumerable.Range(0, 50).Select(i => IsFactoryCalledAsync($"key_{i}")));
        Assert.All(results, Assert.True);
    }

    #endregion

    #region Key Prefix Isolation Tests

    [Fact]
    public async Task GetOrSetAsync_WithDifferentPrefixes_IsolatesCacheEntries()
    {
        var sharedCache = new MockDistributedCache();
        var manager1 = new HybridCacheManager(sharedCache, new DeliveryCacheOptions { KeyPrefix = "client1" });
        var manager2 = new HybridCacheManager(sharedCache, new DeliveryCacheOptions { KeyPrefix = "client2" });

        var value1 = new TestCacheValue { Id = 1, Name = "Client1Value" };
        var value2 = new TestCacheValue { Id = 2, Name = "Client2Value" };

        await PopulateCache("same_key", value1, [], manager1);
        await PopulateCache("same_key", value2, [], manager2);

        var result1 = await GetCachedValue<TestCacheValue>("same_key", manager1);
        var result2 = await GetCachedValue<TestCacheValue>("same_key", manager2);

        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.Equal(1, result1.Id);
        Assert.Equal("Client1Value", result1.Name);
        Assert.Equal(2, result2.Id);
        Assert.Equal("Client2Value", result2.Name);
    }

    [Fact]
    public async Task InvalidateAsync_WithDifferentPrefixes_OnlyAffectsOwnEntries()
    {
        var sharedCache = new MockDistributedCache();
        var manager1 = new HybridCacheManager(sharedCache, new DeliveryCacheOptions { KeyPrefix = "client1" });
        var manager2 = new HybridCacheManager(sharedCache, new DeliveryCacheOptions { KeyPrefix = "client2" });

        var dependency = "same_dep";
        var value1 = new TestCacheValue { Id = 1, Name = "Client1Value" };
        var value2 = new TestCacheValue { Id = 2, Name = "Client2Value" };

        await PopulateCache("same_key", value1, [dependency], manager1);
        await PopulateCache("same_key", value2, [dependency], manager2);

        await manager1.InvalidateAsync(default, dependency);

        Assert.True(await IsFactoryCalledAsync("same_key", manager1));
        Assert.False(await IsFactoryCalledAsync("same_key", manager2));
    }

    [Fact]
    public async Task GetOrSetAsync_WithDifferentPrefixes_DoesNotCrossContaminate()
    {
        var sharedCache = new MockDistributedCache();
        var manager1 = new HybridCacheManager(sharedCache, new DeliveryCacheOptions { KeyPrefix = "client1" });
        var manager2 = new HybridCacheManager(sharedCache, new DeliveryCacheOptions { KeyPrefix = "client2" });

        var value = new TestCacheValue { Id = 1, Name = "OnlyInClient1" };
        await PopulateCache("unique_key", value, [], manager1);

        Assert.False(await IsFactoryCalledAsync("unique_key", manager1));
        Assert.True(await IsFactoryCalledAsync("unique_key", manager2));
    }

    [Fact]
    public async Task GetOrSetAsync_WithNullPrefix_UsesUnprefixedKeys()
    {
        var sharedCache = new MockDistributedCache();
        var managerNoPrefix = new HybridCacheManager(sharedCache, new DeliveryCacheOptions { KeyPrefix = null });
        var managerWithPrefix = new HybridCacheManager(sharedCache, new DeliveryCacheOptions { KeyPrefix = "prefixed" });

        var value1 = new TestCacheValue { Id = 1, Name = "NoPrefix" };
        var value2 = new TestCacheValue { Id = 2, Name = "WithPrefix" };

        await PopulateCache("test_key", value1, [], managerNoPrefix);
        await PopulateCache("test_key", value2, [], managerWithPrefix);

        var result1 = await GetCachedValue<TestCacheValue>("test_key", managerNoPrefix);
        var result2 = await GetCachedValue<TestCacheValue>("test_key", managerWithPrefix);

        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.Equal(1, result1.Id);
        Assert.Equal(2, result2.Id);
    }

    [Fact]
    public async Task InvalidateAsync_WithSharedDependencyName_OnlyInvalidatesOwnPrefix()
    {
        var sharedCache = new MockDistributedCache();
        var manager1 = new HybridCacheManager(sharedCache, new DeliveryCacheOptions { KeyPrefix = "prod" });
        var manager2 = new HybridCacheManager(sharedCache, new DeliveryCacheOptions { KeyPrefix = "preview" });

        var dependency = "content_type_article";

        await PopulateCache("item1", new TestCacheValue { Id = 1 }, [dependency], manager1);
        await PopulateCache("item2", new TestCacheValue { Id = 2 }, [dependency], manager1);
        await PopulateCache("item1", new TestCacheValue { Id = 10 }, [dependency], manager2);
        await PopulateCache("item2", new TestCacheValue { Id = 20 }, [dependency], manager2);

        await manager1.InvalidateAsync(default, dependency);

        Assert.True(await IsFactoryCalledAsync("item1", manager1));
        Assert.True(await IsFactoryCalledAsync("item2", manager1));
        Assert.False(await IsFactoryCalledAsync("item1", manager2));
        Assert.False(await IsFactoryCalledAsync("item2", manager2));
    }

    [Fact]
    public async Task ConcurrentOperations_WithDifferentPrefixes_MaintainsIsolation()
    {
        var sharedCache = new MockDistributedCache();
        var manager1 = new HybridCacheManager(sharedCache, new DeliveryCacheOptions { KeyPrefix = "client1" });
        var manager2 = new HybridCacheManager(sharedCache, new DeliveryCacheOptions { KeyPrefix = "client2" });

        var dependency = "shared_dep_name";

        var tasks1 = Enumerable.Range(0, 25)
            .Select(i => PopulateCache($"key_{i}", new TestCacheValue { Id = i }, [dependency], manager1));
        var tasks2 = Enumerable.Range(0, 25)
            .Select(i => PopulateCache($"key_{i}", new TestCacheValue { Id = i + 100 }, [dependency], manager2));

        await Task.WhenAll(tasks1.Concat(tasks2));

        await manager1.InvalidateAsync(default, dependency);

        var verify1 = await Task.WhenAll(Enumerable.Range(0, 25)
            .Select(i => IsFactoryCalledAsync($"key_{i}", manager1)));
        var verify2 = await Task.WhenAll(Enumerable.Range(0, 25)
            .Select(i => IsFactoryCalledAsync($"key_{i}", manager2)));

        Assert.All(verify1, Assert.True);
        Assert.All(verify2, Assert.False);
    }

    [Fact]
    public async Task Constructor_WithKeyPrefix_IsolatesEntries()
    {
        var cache = new MockDistributedCache();
        var manager = new HybridCacheManager(cache, new DeliveryCacheOptions { KeyPrefix = "my-prefix" });
        var defaultManager = new HybridCacheManager(cache, new DeliveryCacheOptions());

        await PopulateCache("test", new TestCacheValue { Id = 1 }, [], manager);
        Assert.False(await IsFactoryCalledAsync("test", manager));
        Assert.True(await IsFactoryCalledAsync("test", defaultManager));
    }

    #endregion

    #region Test Helpers

    private Task PopulateCache(string key, TestCacheValue value, string[] dependencies, IDeliveryCacheManager? manager = null)
        => (manager ?? _cacheManager).GetOrSetAsync(key, _ =>
            Task.FromResult<CacheEntry<TestCacheValue>?>(
                new CacheEntry<TestCacheValue>(value, dependencies)));

    private async Task<bool> IsFactoryCalledAsync(string key, IDeliveryCacheManager? manager = null)
    {
        var factoryCalled = false;
        await (manager ?? _cacheManager).GetOrSetAsync<TestCacheValue>(key, _ =>
        {
            factoryCalled = true;
            return Task.FromResult<CacheEntry<TestCacheValue>?>(null);
        });
        return factoryCalled;
    }

    private async Task<T?> GetCachedValue<T>(string key, IDeliveryCacheManager? manager = null) where T : class
    {
        var cacheResult = await (manager ?? _cacheManager).GetOrSetAsync<T>(key, _ =>
            Task.FromResult<CacheEntry<T>?>(null));
        return cacheResult?.Value;
    }

    private static async Task<bool> WaitUntilAsync(Func<Task<bool>> condition, TimeSpan timeout, TimeSpan pollInterval)
    {
        if (await condition().ConfigureAwait(false))
            return true;

        var sw = Stopwatch.StartNew();
        using var timer = new PeriodicTimer(pollInterval);
        while (sw.Elapsed < timeout)
        {
            if (await condition().ConfigureAwait(false))
                return true;

            if (!await timer.WaitForNextTickAsync().ConfigureAwait(false))
                break;
        }

        return await condition().ConfigureAwait(false);
    }

    #endregion

    #region Test Helper Classes

    private class TestCacheValue
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }

    private class ComplexCacheValue
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public NestedValue? Nested { get; set; }
        public List<int>? Items { get; set; }
    }

    private class NestedValue
    {
        public string? Description { get; set; }
        public string[]? Tags { get; set; }
    }

    private class CircularReferenceValue
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public CircularReferenceValue? Self { get; set; }
    }

    /// <summary>
    /// Simple in-memory implementation of IDistributedCache for testing.
    /// </summary>
    private class MockDistributedCache : IDistributedCache
    {
        private readonly Dictionary<string, byte[]> _cache = [];
        private readonly object _lock = new();

        public byte[]? Get(string key)
        {
            lock (_lock)
            {
                return _cache.TryGetValue(key, out var value) ? value : null;
            }
        }

        public Task<byte[]?> GetAsync(string key, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            return Task.FromResult(Get(key));
        }

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            lock (_lock)
            {
                _cache[key] = value;
            }
        }

        public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            Set(key, value, options);
            return Task.CompletedTask;
        }

        public void Refresh(string key) { }

        public Task RefreshAsync(string key, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }

        public void Remove(string key)
        {
            lock (_lock)
            {
                _cache.Remove(key);
            }
        }

        public Task RemoveAsync(string key, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            Remove(key);
            return Task.CompletedTask;
        }
    }

    #endregion
}
