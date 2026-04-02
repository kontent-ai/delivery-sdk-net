using System.Diagnostics;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.Caching;
using Microsoft.Extensions.Caching.Memory;

namespace Kontent.Ai.Delivery.Tests.Caching;

/// <summary>
/// Comprehensive tests for MemoryCacheManager implementation.
/// Tests cover: basic operations, dependency tracking, invalidation, concurrency, resource management, and error handling.
/// </summary>
public class MemoryCacheManagerTests : IDisposable
{
    private readonly IMemoryCache _memoryCache;
    private readonly MemoryCacheManager _cacheManager;

    public MemoryCacheManagerTests()
    {
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        _cacheManager = new MemoryCacheManager(_memoryCache, new DeliveryCacheOptions { DefaultExpiration = TimeSpan.FromMinutes(5) });
    }

    public void Dispose()
    {
        _cacheManager?.Dispose();
        _memoryCache?.Dispose();
    }

    #region Basic Operations Tests

    [Fact]
    public async Task GetOrSetAsync_CacheMiss_CallsFactory()
    {
        var factoryCalled = false;
        var value = new TestCacheValue { Id = 1, Name = "Test" };

        var result = await _cacheManager.GetOrSetAsync("test_key", _ =>
        {
            factoryCalled = true;
            return Task.FromResult<CacheEntry<TestCacheValue>?>(
                new CacheEntry<TestCacheValue>(value, ["dep1"]));
        });

        Assert.True(factoryCalled);
        Assert.NotNull(result);
        Assert.Equal(1, result.Value.Id);
        Assert.Equal("Test", result.Value.Name);
    }

    [Fact]
    public async Task GetOrSetAsync_CacheHit_DoesNotCallFactory()
    {
        var value = new TestCacheValue { Id = 1, Name = "Test" };

        // First call populates cache
        await _cacheManager.GetOrSetAsync("test_key", _ =>
            Task.FromResult<CacheEntry<TestCacheValue>?>(
                new CacheEntry<TestCacheValue>(value, ["dep1"])));

        // Second call should be a cache hit
        var factoryCalled = false;
        var result = await _cacheManager.GetOrSetAsync("test_key", _ =>
        {
            factoryCalled = true;
            return Task.FromResult<CacheEntry<TestCacheValue>?>(null);
        });

        Assert.False(factoryCalled);
        Assert.NotNull(result);
        Assert.Equal(1, result.Value.Id);
    }

    [Fact]
    public async Task GetOrSetAsync_FactoryReturnsNull_ReturnsNull()
    {
        var result = await _cacheManager.GetOrSetAsync<TestCacheValue>("test_key", _ =>
            Task.FromResult<CacheEntry<TestCacheValue>?>(null));

        Assert.Null(result);
    }

    [Fact]
    public async Task GetOrSetAsync_FactoryReturnsNull_DoesNotCache()
    {
        // First call: factory returns null
        await _cacheManager.GetOrSetAsync<TestCacheValue>("test_key", _ =>
            Task.FromResult<CacheEntry<TestCacheValue>?>(null));

        // Second call: factory should be called again
        var factoryCalled = false;
        var value = new TestCacheValue { Id = 1, Name = "Test" };
        var result = await _cacheManager.GetOrSetAsync("test_key", _ =>
        {
            factoryCalled = true;
            return Task.FromResult<CacheEntry<TestCacheValue>?>(
                new CacheEntry<TestCacheValue>(value, []));
        });

        Assert.True(factoryCalled);
        Assert.NotNull(result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetOrSetAsync_InvalidKey_StillCallsFactory(string? cacheKey)
    {
        var factoryCalled = false;
        var value = new TestCacheValue { Id = 1, Name = "Test" };

        var result = await _cacheManager.GetOrSetAsync(cacheKey!, _ =>
        {
            factoryCalled = true;
            return Task.FromResult<CacheEntry<TestCacheValue>?>(
                new CacheEntry<TestCacheValue>(value, []));
        });

        Assert.True(factoryCalled);
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetOrSetAsync_EmptyDependencies_DoesNotThrow()
    {
        var value = new TestCacheValue { Id = 1, Name = "Test" };

        var result = await _cacheManager.GetOrSetAsync("test_key", _ =>
            Task.FromResult<CacheEntry<TestCacheValue>?>(
                new CacheEntry<TestCacheValue>(value, Array.Empty<string>())));

        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetOrSetAsync_SecondCallOverwritesWithNewFactory()
    {
        var value1 = new TestCacheValue { Id = 1, Name = "First" };
        var value2 = new TestCacheValue { Id = 2, Name = "Second" };

        await _cacheManager.GetOrSetAsync("test_key", _ =>
            Task.FromResult<CacheEntry<TestCacheValue>?>(
                new CacheEntry<TestCacheValue>(value1, [])));

        // Second call is a cache hit, returns the cached first value
        var result = await _cacheManager.GetOrSetAsync("test_key", _ =>
            Task.FromResult<CacheEntry<TestCacheValue>?>(
                new CacheEntry<TestCacheValue>(value2, [])));

        Assert.NotNull(result);
        Assert.Equal(value1.Id, result.Value.Id);
    }

    [Fact]
    public async Task GetOrSetAsync_WithCustomExpiration_ExpiresAfterTimeout()
    {
        var value = new TestCacheValue { Id = 1, Name = "Test" };
        var expiration = TimeSpan.FromMilliseconds(100);

        await _cacheManager.GetOrSetAsync("test_key", _ =>
            Task.FromResult<CacheEntry<TestCacheValue>?>(
                new CacheEntry<TestCacheValue>(value, [])),
            expiration);

        var expired = await WaitUntilAsync(
            async () =>
            {
                var factoryCalled = false;
                await _cacheManager.GetOrSetAsync("test_key", _ =>
                {
                    factoryCalled = true;
                    return Task.FromResult<CacheEntry<TestCacheValue>?>(null);
                });
                return factoryCalled;
            },
            timeout: TimeSpan.FromSeconds(2),
            pollInterval: TimeSpan.FromMilliseconds(20));

        Assert.True(expired);
    }

    [Fact]
    public async Task PurgeAsync_RemovesAllExistingEntries()
    {
        await PopulateCache("k1", new TestCacheValue { Id = 1, Name = "One" }, ["dep1"]);
        await PopulateCache("k2", new TestCacheValue { Id = 2, Name = "Two" }, ["dep2"]);

        await ((IDeliveryCachePurger)_cacheManager).PurgeAsync();

        Assert.True(await IsFactoryCalledAsync("k1"));
        Assert.True(await IsFactoryCalledAsync("k2"));
    }

    [Fact]
    public async Task PurgeAsync_WithAllowFailSafe_ExpiresEntriesAndDoesNotAffectNewEntries()
    {
        await PopulateCache("k1", new TestCacheValue { Id = 1, Name = "One" }, ["dep1"]);
        await PopulateCache("k2", new TestCacheValue { Id = 2, Name = "Two" }, ["dep2"]);

        await ((IDeliveryCachePurger)_cacheManager).PurgeAsync(allowFailSafe: true);

        Assert.True(await IsFactoryCalledAsync("k1"));
        Assert.True(await IsFactoryCalledAsync("k2"));

        await PopulateCache("k3", new TestCacheValue { Id = 3, Name = "Three" }, ["dep3"]);
        Assert.False(await IsFactoryCalledAsync("k3"));
    }

    [Fact]
    public async Task PurgeAsync_DoesNotAffectEntriesCreatedAfterPurge()
    {
        await PopulateCache("old", new TestCacheValue { Id = 1, Name = "Old" }, ["dep"]);

        await ((IDeliveryCachePurger)_cacheManager).PurgeAsync();
        await PopulateCache("new", new TestCacheValue { Id = 2, Name = "New" }, ["dep"]);

        Assert.True(await IsFactoryCalledAsync("old"));
        Assert.False(await IsFactoryCalledAsync("new"));
    }

    [Fact]
    public async Task ExpiredDependencyEntry_DoesNotBreakFutureInvalidation()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions
        {
            ExpirationScanFrequency = TimeSpan.FromMilliseconds(10)
        });
        using var manager = new MemoryCacheManager(cache, new DeliveryCacheOptions());

        var dependency = "dep1";
        var expiration = TimeSpan.FromMilliseconds(50);

        await PopulateCache(manager, "test_key", new TestCacheValue { Id = 1, Name = "Test" }, [dependency], expiration);

        var expired = await WaitUntilAsync(
            async () =>
            {
                cache.Compact(1.0);
                return await IsFactoryCalledAsync(manager, "test_key");
            },
            timeout: TimeSpan.FromSeconds(2),
            pollInterval: TimeSpan.FromMilliseconds(20));

        Assert.True(expired);

        await PopulateCache(manager, "next_key", new TestCacheValue { Id = 2, Name = "Next" }, [dependency]);
        Assert.False(await IsFactoryCalledAsync(manager, "next_key"));

        await manager.InvalidateAsync([dependency]);
        Assert.True(await IsFactoryCalledAsync(manager, "next_key"));
    }

    #endregion

    #region Dependency Tracking Tests

    [Fact]
    public async Task GetOrSetAsync_WithDependencies_TracksCorrectly()
    {
        var value = new TestCacheValue { Id = 1, Name = "Test" };

        await PopulateCache("test_key", value, ["dep1", "dep2", "dep3"]);

        Assert.False(await IsFactoryCalledAsync("test_key"));
    }

    [Fact]
    public async Task GetOrSetAsync_WithDuplicateDependencies_HandlesCorrectly()
    {
        var value = new TestCacheValue { Id = 1, Name = "Test" };

        await PopulateCache("test_key", value, ["dep1", "dep1", "dep2", "dep2"]);

        Assert.False(await IsFactoryCalledAsync("test_key"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetOrSetAsync_WithIgnoredDependencyValue_IgnoresInvalidDependency(string? ignoredDependency)
    {
        var value = new TestCacheValue { Id = 1, Name = "Test" };

        await PopulateCache("test_key", value, ["dep1", ignoredDependency!, "dep2"]);

        Assert.False(await IsFactoryCalledAsync("test_key"));
    }

    #endregion

    #region Invalidation Tests

    [Fact]
    public async Task InvalidateAsync_RemovesCacheEntry_ReturnsTrue()
    {
        var dependency = "dep1";
        await PopulateCache("test_key", new TestCacheValue { Id = 1, Name = "Test" }, [dependency]);

        var result = await _cacheManager.InvalidateAsync([dependency]);

        Assert.True(result);
        Assert.True(await IsFactoryCalledAsync("test_key"));
    }

    [Fact]
    public async Task InvalidateAsync_NonExistentDependency_ReturnsTrueAndPreservesExistingEntries()
    {
        await PopulateCache("existing_key", new TestCacheValue { Id = 10, Name = "Existing" }, ["existing_dep"]);

        var result = await _cacheManager.InvalidateAsync(["non_existent_dep"]);

        Assert.True(result);
        Assert.False(await IsFactoryCalledAsync("existing_key"));
    }

    [Fact]
    public async Task InvalidateAsync_NullDependencies_ReturnsTrue()
    {
        var result = await _cacheManager.InvalidateAsync(null!);
        Assert.True(result);
    }

    [Fact]
    public async Task InvalidateAsync_EmptyDependencies_ReturnsTrue()
    {
        var result = await _cacheManager.InvalidateAsync([]);
        Assert.True(result);
    }

    [Fact]
    public async Task InvalidateAsync_MultipleDependencies_RemovesAllAffected()
    {
        var value = new TestCacheValue { Id = 1, Name = "Test" };
        await PopulateCache("key1", value, ["dep1"]);
        await PopulateCache("key2", value, ["dep2"]);
        await PopulateCache("key3", value, ["dep3"]);

        await _cacheManager.InvalidateAsync(["dep1", "dep2"]);

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

        await _cacheManager.InvalidateAsync([sharedDependency]);

        Assert.True(await IsFactoryCalledAsync("key1"));
        Assert.True(await IsFactoryCalledAsync("key2"));
        Assert.False(await IsFactoryCalledAsync("key3"));
    }

    [Fact]
    public async Task InvalidateAsync_IdempotentOperation_CanBeCalledMultipleTimes()
    {
        var dependency = "dep1";
        await PopulateCache("test_key", new TestCacheValue { Id = 1, Name = "Test" }, [dependency]);

        await _cacheManager.InvalidateAsync([dependency]);
        await _cacheManager.InvalidateAsync([dependency]);
        await _cacheManager.InvalidateAsync([dependency]);

        Assert.True(await IsFactoryCalledAsync("test_key"));
    }

    [Fact]
    public async Task InvalidateAsync_WithCaseVariantKeys_RemovesAllMatchingEntries()
    {
        var dependency = "dep1";
        await PopulateCache("Key", new TestCacheValue { Id = 1, Name = "Upper" }, [dependency]);
        await PopulateCache("key", new TestCacheValue { Id = 2, Name = "Lower" }, [dependency]);

        Assert.False(await IsFactoryCalledAsync("Key"));
        Assert.False(await IsFactoryCalledAsync("key"));

        await _cacheManager.InvalidateAsync([dependency]);

        Assert.True(await IsFactoryCalledAsync("Key"));
        Assert.True(await IsFactoryCalledAsync("key"));
    }

    #endregion

    #region Concurrency Tests

    [Fact]
    public async Task ConcurrentGetOrSet_DoesNotThrow()
    {
        var value = new TestCacheValue { Id = 1, Name = "Test" };
        await PopulateCache("test_key", value, []);

        var tasks = Enumerable.Range(0, 100)
            .Select(_ => _cacheManager.GetOrSetAsync("test_key", _ =>
                Task.FromResult<CacheEntry<TestCacheValue>?>(
                    new CacheEntry<TestCacheValue>(value, []))))
            .ToList();

        var results = await Task.WhenAll(tasks);

        Assert.All(results, Assert.NotNull);
    }

    [Fact]
    public async Task ConcurrentGetOrSet_WithSameDependency_DoesNotCorruptState()
    {
        var sharedDependency = "shared_dep";
        var tasks = Enumerable.Range(0, 50)
            .Select(i => PopulateCache($"key_{i}", new TestCacheValue { Id = i, Name = $"Test_{i}" }, [sharedDependency]))
            .ToList();

        await Task.WhenAll(tasks);

        await _cacheManager.InvalidateAsync([sharedDependency]);

        var verifyTasks = Enumerable.Range(0, 50)
            .Select(i => IsFactoryCalledAsync($"key_{i}"))
            .ToList();

        var results = await Task.WhenAll(verifyTasks);

        Assert.All(results, r => Assert.True(r));
    }

    [Fact]
    public async Task ConcurrentGetOrSetAndInvalidate_DoesNotThrow()
    {
        var dependency = "dep1";
        var setTasks = Enumerable.Range(0, 25)
            .Select(i => PopulateCache($"key_{i}", new TestCacheValue { Id = i, Name = $"Test_{i}" }, [dependency]))
            .ToList();

        var invalidateTasks = Enumerable.Range(0, 25)
            .Select(async _ =>
            {
                await Task.Yield();
                await _cacheManager.InvalidateAsync([dependency]);
            })
            .ToList();

        var exception = await Record.ExceptionAsync(async () => await Task.WhenAll(setTasks.Concat<Task>(invalidateTasks)));
        Assert.Null(exception);
    }

    [Fact]
    public async Task ConcurrentInvalidate_SameDependency_DoesNotThrow()
    {
        var dependency = "dep1";
        await PopulateCache("test_key", new TestCacheValue { Id = 1, Name = "Test" }, [dependency]);

        var tasks = Enumerable.Range(0, 50)
            .Select(_ => _cacheManager.InvalidateAsync([dependency]))
            .ToList();

        await Task.WhenAll(tasks);

        Assert.True(await IsFactoryCalledAsync("test_key"));
    }

    [Fact]
    public async Task ReverseIndex_ConcurrentCleanupAndSet_PreservesDependencyInvalidation()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions
        {
            ExpirationScanFrequency = TimeSpan.FromMilliseconds(10)
        });
        using var manager = new MemoryCacheManager(cache, new DeliveryCacheOptions());

        const string dependency = "dep_race";
        const string expiringKey = "expiring_key";
        const string stableKey = "stable_key";

        await PopulateCache(manager, expiringKey, new TestCacheValue { Id = 1, Name = "Expiring" }, [dependency], TimeSpan.FromMilliseconds(40));

        var churnTask = Task.Run(async () =>
        {
            for (var i = 0; i < 50; i++)
            {
                await PopulateCache(manager, stableKey, new TestCacheValue { Id = i, Name = $"Stable_{i}" }, [dependency], TimeSpan.FromMinutes(1));
                await Task.Yield();
            }
        });

        var expired = await WaitUntilAsync(
            async () =>
            {
                cache.Compact(1.0);
                return await IsFactoryCalledAsync(manager, expiringKey);
            },
            timeout: TimeSpan.FromSeconds(2),
            pollInterval: TimeSpan.FromMilliseconds(20));

        await churnTask;
        Assert.True(expired);

        await manager.InvalidateAsync([dependency]);
        Assert.True(await IsFactoryCalledAsync(manager, stableKey));
    }

    #endregion

    #region Disposal Tests

    [Fact]
    public async Task Dispose_DisposesResources()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var manager = new MemoryCacheManager(cache, new DeliveryCacheOptions());

        await PopulateCache(manager, "test_key", new TestCacheValue { Id = 1, Name = "Test" }, ["dep1"]);

        manager.Dispose();

        await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            manager.GetOrSetAsync("test_key", _ =>
                Task.FromResult<CacheEntry<TestCacheValue>?>(null)));
    }

    [Fact]
    public void Dispose_CalledMultipleTimes_DoesNotThrow()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var manager = new MemoryCacheManager(cache, new DeliveryCacheOptions());

        var exception = Record.Exception(() =>
        {
            manager.Dispose();
            manager.Dispose();
            manager.Dispose();
        });

        Assert.Null(exception);
    }

    [Fact]
    public async Task Dispose_WithPendingOperations_CleansUpCorrectly()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var manager = new MemoryCacheManager(cache, new DeliveryCacheOptions());

        await PopulateCache(manager, "test_key", new TestCacheValue { Id = 1, Name = "Test" }, ["dep1", "dep2", "dep3"]);

        manager.Dispose();

        Assert.True(true);
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
        await PopulateCache("test_key", new TestCacheValue { Id = 1, Name = "Test" }, [dependency]);
        await _cacheManager.InvalidateAsync([dependency]);
        Assert.True(await IsFactoryCalledAsync("test_key"));
    }

    [Fact]
    public async Task GetOrSetAsync_ManyDependencies_Succeeds()
    {
        var dependencies = Enumerable.Range(0, 100).Select(i => $"dep_{i}").ToArray();
        await PopulateCache("test_key", new TestCacheValue { Id = 1, Name = "Test" }, dependencies);
        Assert.False(await IsFactoryCalledAsync("test_key"));
    }

    [Fact]
    public async Task InvalidateAsync_ManyDependencies_Succeeds()
    {
        var keys = Enumerable.Range(0, 50).Select(i => $"key_{i}").ToArray();
        var value = new TestCacheValue { Id = 1, Name = "Test" };

        foreach (var key in keys)
        {
            await PopulateCache(key, value, [$"dep_{key}"]);
        }

        var dependenciesToInvalidate = keys.Select(k => $"dep_{k}").ToArray();
        await _cacheManager.InvalidateAsync(dependenciesToInvalidate);

        foreach (var key in keys)
        {
            Assert.True(await IsFactoryCalledAsync(key));
        }
    }

    [Fact]
    public async Task GetOrSetAsync_ComplexObject_WithNestedProperties_Succeeds()
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

    #endregion

    #region Key Prefix Isolation Tests

    [Fact]
    public async Task GetOrSetAsync_WithDifferentPrefixes_IsolatesCacheEntries()
    {
        var sharedCache = new MemoryCache(new MemoryCacheOptions());
        using var manager1 = new MemoryCacheManager(sharedCache, new DeliveryCacheOptions { KeyPrefix = "client1" });
        using var manager2 = new MemoryCacheManager(sharedCache, new DeliveryCacheOptions { KeyPrefix = "client2" });

        var key = "same_key";
        var value1 = new TestCacheValue { Id = 1, Name = "Client1Value" };
        var value2 = new TestCacheValue { Id = 2, Name = "Client2Value" };

        await PopulateCache(manager1, key, value1, []);
        await PopulateCache(manager2, key, value2, []);

        var result1 = await GetCachedValue<TestCacheValue>(manager1, key);
        var result2 = await GetCachedValue<TestCacheValue>(manager2, key);

        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.Equal(1, result1.Id);
        Assert.Equal("Client1Value", result1.Name);
        Assert.Equal(2, result2.Id);
        Assert.Equal("Client2Value", result2.Name);

        sharedCache.Dispose();
    }

    [Fact]
    public async Task InvalidateAsync_WithDifferentPrefixes_OnlyAffectsOwnEntries()
    {
        var sharedCache = new MemoryCache(new MemoryCacheOptions());
        using var manager1 = new MemoryCacheManager(sharedCache, new DeliveryCacheOptions { KeyPrefix = "client1" });
        using var manager2 = new MemoryCacheManager(sharedCache, new DeliveryCacheOptions { KeyPrefix = "client2" });

        var key = "same_key";
        var dependency = "same_dep";

        await PopulateCache(manager1, key, new TestCacheValue { Id = 1, Name = "Client1Value" }, [dependency]);
        await PopulateCache(manager2, key, new TestCacheValue { Id = 2, Name = "Client2Value" }, [dependency]);

        await manager1.InvalidateAsync([dependency]);

        Assert.True(await IsFactoryCalledAsync(manager1, key));
        Assert.False(await IsFactoryCalledAsync(manager2, key));

        sharedCache.Dispose();
    }

    [Fact]
    public async Task GetOrSetAsync_WithDifferentPrefixes_DoesNotCrossContaminate()
    {
        var sharedCache = new MemoryCache(new MemoryCacheOptions());
        using var manager1 = new MemoryCacheManager(sharedCache, new DeliveryCacheOptions { KeyPrefix = "client1" });
        using var manager2 = new MemoryCacheManager(sharedCache, new DeliveryCacheOptions { KeyPrefix = "client2" });

        var key = "unique_key";
        await PopulateCache(manager1, key, new TestCacheValue { Id = 1, Name = "OnlyInClient1" }, []);

        Assert.False(await IsFactoryCalledAsync(manager1, key));
        Assert.True(await IsFactoryCalledAsync(manager2, key));

        sharedCache.Dispose();
    }

    [Fact]
    public async Task GetOrSetAsync_WithNullPrefix_UsesUnprefixedKeys()
    {
        var sharedCache = new MemoryCache(new MemoryCacheOptions());
        using var managerNoPrefix = new MemoryCacheManager(sharedCache, new DeliveryCacheOptions { KeyPrefix = null });
        using var managerWithPrefix = new MemoryCacheManager(sharedCache, new DeliveryCacheOptions { KeyPrefix = "prefixed" });

        var key = "test_key";
        await PopulateCache(managerNoPrefix, key, new TestCacheValue { Id = 1, Name = "NoPrefix" }, []);
        await PopulateCache(managerWithPrefix, key, new TestCacheValue { Id = 2, Name = "WithPrefix" }, []);

        var result1 = await GetCachedValue<TestCacheValue>(managerNoPrefix, key);
        var result2 = await GetCachedValue<TestCacheValue>(managerWithPrefix, key);

        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.Equal(1, result1.Id);
        Assert.Equal(2, result2.Id);

        sharedCache.Dispose();
    }

    [Fact]
    public async Task InvalidateAsync_WithSharedDependencyName_OnlyInvalidatesOwnPrefix()
    {
        var sharedCache = new MemoryCache(new MemoryCacheOptions());
        using var manager1 = new MemoryCacheManager(sharedCache, new DeliveryCacheOptions { KeyPrefix = "prod" });
        using var manager2 = new MemoryCacheManager(sharedCache, new DeliveryCacheOptions { KeyPrefix = "preview" });

        var dependency = "content_type_article";

        await PopulateCache(manager1, "item1", new TestCacheValue { Id = 1 }, [dependency]);
        await PopulateCache(manager1, "item2", new TestCacheValue { Id = 2 }, [dependency]);
        await PopulateCache(manager2, "item1", new TestCacheValue { Id = 10 }, [dependency]);
        await PopulateCache(manager2, "item2", new TestCacheValue { Id = 20 }, [dependency]);

        await manager1.InvalidateAsync([dependency]);

        Assert.True(await IsFactoryCalledAsync(manager1, "item1"));
        Assert.True(await IsFactoryCalledAsync(manager1, "item2"));
        Assert.False(await IsFactoryCalledAsync(manager2, "item1"));
        Assert.False(await IsFactoryCalledAsync(manager2, "item2"));

        sharedCache.Dispose();
    }

    [Fact]
    public async Task ConcurrentOperations_WithDifferentPrefixes_MaintainsIsolation()
    {
        var sharedCache = new MemoryCache(new MemoryCacheOptions());
        using var manager1 = new MemoryCacheManager(sharedCache, new DeliveryCacheOptions { KeyPrefix = "client1" });
        using var manager2 = new MemoryCacheManager(sharedCache, new DeliveryCacheOptions { KeyPrefix = "client2" });

        var dependency = "shared_dep_name";

        var tasks1 = Enumerable.Range(0, 25)
            .Select(i => PopulateCache(manager1, $"key_{i}", new TestCacheValue { Id = i }, [dependency]));
        var tasks2 = Enumerable.Range(0, 25)
            .Select(i => PopulateCache(manager2, $"key_{i}", new TestCacheValue { Id = i + 100 }, [dependency]));

        await Task.WhenAll(tasks1.Concat(tasks2));

        await manager1.InvalidateAsync([dependency]);

        var verify1 = await Task.WhenAll(Enumerable.Range(0, 25)
            .Select(i => IsFactoryCalledAsync(manager1, $"key_{i}")));
        var verify2 = await Task.WhenAll(Enumerable.Range(0, 25)
            .Select(i => IsFactoryCalledAsync(manager2, $"key_{i}")));

        Assert.All(verify1, r => Assert.True(r));
        Assert.All(verify2, r => Assert.False(r));

        sharedCache.Dispose();
    }

    #endregion

    #region Fail-Safe Tests

    [Fact]
    public async Task FailSafe_Enabled_ServesStaleEntryAfterExpiration()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        using var manager = new MemoryCacheManager(cache, new DeliveryCacheOptions
        {
            DefaultExpiration = TimeSpan.FromMilliseconds(100),
            IsFailSafeEnabled = true,
            FailSafeMaxDuration = TimeSpan.FromMinutes(5),
            FailSafeThrottleDuration = TimeSpan.FromSeconds(1)
        });

        var value = new TestCacheValue { Id = 42, Name = "Stale" };
        await PopulateCache(manager, "failsafe_key", value, ["dep1"]);

        // Poll until the TTL expires (factory is called) and verify fail-safe returns the stale value.
        TestCacheValue? failSafeResult = null;
        var expired = await WaitUntilAsync(
            async () =>
            {
                var factoryCalled = false;
                var r = await manager.GetOrSetAsync<TestCacheValue>("failsafe_key", _ =>
                {
                    factoryCalled = true;
                    throw new InvalidOperationException("Simulated API failure");
                });
                if (factoryCalled)
                {
                    failSafeResult = r?.Value;
                }
                return factoryCalled;
            },
            timeout: TimeSpan.FromSeconds(2),
            pollInterval: TimeSpan.FromMilliseconds(20));

        Assert.True(expired, "Cache entry did not expire within timeout");
        Assert.NotNull(failSafeResult);
        Assert.Equal(42, failSafeResult.Id);
        Assert.Equal("Stale", failSafeResult.Name);
    }

    [Fact]
    public async Task FailSafe_Enabled_ServesStaleEntryWhenFactoryReturnsNull()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        using var manager = new MemoryCacheManager(cache, new DeliveryCacheOptions
        {
            DefaultExpiration = TimeSpan.FromMilliseconds(100),
            IsFailSafeEnabled = true,
            FailSafeMaxDuration = TimeSpan.FromMinutes(5),
            FailSafeThrottleDuration = TimeSpan.FromSeconds(1)
        });

        var value = new TestCacheValue { Id = 99, Name = "StaleFromNull" };
        await PopulateCache(manager, "failsafe_null_key", value, ["dep1"]);

        // Poll until TTL expires (factory is called) and factory returns null.
        TestCacheValue? failSafeResult = null;
        var expired = await WaitUntilAsync(
            async () =>
            {
                var factoryCalled = false;
                var r = await manager.GetOrSetAsync<TestCacheValue>("failsafe_null_key", _ =>
                {
                    factoryCalled = true;
                    return Task.FromResult<CacheEntry<TestCacheValue>?>(null);
                });
                if (factoryCalled)
                {
                    failSafeResult = r?.Value;
                }
                return factoryCalled;
            },
            timeout: TimeSpan.FromSeconds(2),
            pollInterval: TimeSpan.FromMilliseconds(20));

        Assert.True(expired, "Cache entry did not expire within timeout");
        Assert.NotNull(failSafeResult);
        Assert.Equal(99, failSafeResult.Id);
        Assert.Equal("StaleFromNull", failSafeResult.Name);
    }

    [Fact]
    public async Task FailSafe_Disabled_ReturnsNullWhenFactoryReturnsNull()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        using var manager = new MemoryCacheManager(cache, new DeliveryCacheOptions
        {
            DefaultExpiration = TimeSpan.FromMilliseconds(100),
            IsFailSafeEnabled = false
        });

        var value = new TestCacheValue { Id = 50, Name = "NoFailSafe" };
        await PopulateCache(manager, "no_failsafe_null_key", value, ["dep1"]);

        // Poll until TTL expires, factory returns null, verify null is returned.
        TestCacheValue? resultAfterExpiry = null;
        var expired = await WaitUntilAsync(
            async () =>
            {
                var factoryCalled = false;
                var r = await manager.GetOrSetAsync<TestCacheValue>("no_failsafe_null_key", _ =>
                {
                    factoryCalled = true;
                    return Task.FromResult<CacheEntry<TestCacheValue>?>(null);
                });
                if (factoryCalled)
                {
                    resultAfterExpiry = r?.Value;
                }
                return factoryCalled;
            },
            timeout: TimeSpan.FromSeconds(2),
            pollInterval: TimeSpan.FromMilliseconds(20));

        Assert.True(expired, "Cache entry did not expire within timeout");
        Assert.Null(resultAfterExpiry);
    }

    [Fact]
    public async Task FailSafe_Enabled_NoStaleEntry_ReturnsNullWhenFactoryReturnsNull()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        using var manager = new MemoryCacheManager(cache, new DeliveryCacheOptions
        {
            DefaultExpiration = TimeSpan.FromMinutes(5),
            IsFailSafeEnabled = true,
            FailSafeMaxDuration = TimeSpan.FromMinutes(30),
            FailSafeThrottleDuration = TimeSpan.FromSeconds(1)
        });

        // No prior cache entry — factory returns null on first call.
        var result = await manager.GetOrSetAsync<TestCacheValue>("never_cached_key", _ =>
            Task.FromResult<CacheEntry<TestCacheValue>?>(null));

        Assert.Null(result);
    }

    [Fact]
    public async Task FailSafe_Disabled_ReturnsNullAfterExpiration()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        using var manager = new MemoryCacheManager(cache, new DeliveryCacheOptions
        {
            DefaultExpiration = TimeSpan.FromMilliseconds(100),
            IsFailSafeEnabled = false
        });

        var value = new TestCacheValue { Id = 42, Name = "Ephemeral" };
        await PopulateCache(manager, "no_failsafe_key", value, ["dep1"]);

        var expired = await WaitUntilAsync(
            async () => await IsFactoryCalledAsync(manager, "no_failsafe_key"),
            timeout: TimeSpan.FromSeconds(2),
            pollInterval: TimeSpan.FromMilliseconds(20));

        Assert.True(expired);
    }

    #endregion

    #region Jitter Tests

    [Fact]
    public void Jitter_DoesNotThrow()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var exception = Record.Exception(() => new MemoryCacheManager(cache, new DeliveryCacheOptions
        {
            DefaultExpiration = TimeSpan.FromMinutes(5),
            JitterMaxDuration = TimeSpan.FromSeconds(30)
        }));

        Assert.Null(exception);
    }

    #endregion

    #region Test Helpers

    private Task PopulateCache(string key, TestCacheValue value, string[] dependencies, TimeSpan? expiration = null)
        => PopulateCache(_cacheManager, key, value, dependencies, expiration);

    private static Task PopulateCache(IDeliveryCacheManager manager, string key, TestCacheValue value, string[] dependencies, TimeSpan? expiration = null)
        => manager.GetOrSetAsync(key, _ =>
            Task.FromResult<CacheEntry<TestCacheValue>?>(
                new CacheEntry<TestCacheValue>(value, dependencies)),
            expiration);

    private Task<bool> IsFactoryCalledAsync(string key) => IsFactoryCalledAsync(_cacheManager, key);

    private static async Task<bool> IsFactoryCalledAsync(IDeliveryCacheManager manager, string key)
    {
        var factoryCalled = false;
        await manager.GetOrSetAsync<TestCacheValue>(key, _ =>
        {
            factoryCalled = true;
            return Task.FromResult<CacheEntry<TestCacheValue>?>(null);
        });
        return factoryCalled;
    }

    private static async Task<T?> GetCachedValue<T>(IDeliveryCacheManager manager, string key) where T : class
    {
        var result = await manager.GetOrSetAsync<T>(key, _ =>
            Task.FromResult<CacheEntry<T>?>(null));
        return result?.Value;
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

    #region ConfigureFusionCacheOptions Tests

    [Fact]
    public async Task ConfigureFusionCacheOptions_Callback_IsInvoked()
    {
        var callbackInvoked = false;
        var options = new DeliveryCacheOptions
        {
            DefaultExpiration = TimeSpan.FromMinutes(5),
            ConfigureFusionCacheOptions = _ => callbackInvoked = true
        };

        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        using var manager = new MemoryCacheManager(memoryCache, options);

        // The callback should have been invoked during construction
        Assert.True(callbackInvoked);

        // Verify the manager is functional
        var value = await manager.GetOrSetAsync("test_key", _ =>
            Task.FromResult<CacheEntry<TestCacheValue>?>(
                new CacheEntry<TestCacheValue>(new TestCacheValue { Id = 1 }, [])));

        Assert.NotNull(value);
        Assert.Equal(1, value.Value.Id);
    }

    [Fact]
    public void ConfigureFusionCacheOptions_Callback_ReceivesFusionCacheOptions()
    {
        object? receivedOptions = null;
        var options = new DeliveryCacheOptions
        {
            DefaultExpiration = TimeSpan.FromMinutes(5),
            ConfigureFusionCacheOptions = opts => receivedOptions = opts
        };

        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        using var manager = new MemoryCacheManager(memoryCache, options);

        Assert.NotNull(receivedOptions);
        Assert.Equal("ZiggyCreatures.Caching.Fusion.FusionCacheOptions", receivedOptions.GetType().FullName);
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

    #endregion
}
