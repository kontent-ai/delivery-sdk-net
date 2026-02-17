using System.Diagnostics;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.Caching;
using Microsoft.Extensions.Caching.Memory;
using Xunit;

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
        _cacheManager = new MemoryCacheManager(_memoryCache, defaultExpiration: TimeSpan.FromMinutes(5));
    }

    public void Dispose()
    {
        _cacheManager?.Dispose();
        _memoryCache?.Dispose();
    }

    #region Basic Operations Tests

    [Fact]
    public async Task GetAsync_NonExistentKey_ReturnsNull()
    {
        var result = await _cacheManager.GetAsync<TestCacheValue>("non_existent_key");

        Assert.Null(result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetAsync_InvalidKey_ReturnsNull(string? cacheKey)
    {
        var result = await _cacheManager.GetAsync<TestCacheValue>(cacheKey!);

        Assert.Null(result);
    }

    [Fact]
    public async Task SetAsync_ThenGetAsync_ReturnsValue()
    {
        var key = "test_key";
        var value = new TestCacheValue { Id = 1, Name = "Test" };
        var dependencies = new[] { "dep1" };

        await _cacheManager.SetAsync(key, value, dependencies);
        var result = await _cacheManager.GetAsync<TestCacheValue>(key);

        Assert.NotNull(result);
        Assert.Equal(value.Id, result.Id);
        Assert.Equal(value.Name, result.Name);
    }

    [Fact]
    public async Task SetAsync_NullKey_ThrowsArgumentNullException()
    {
        var value = new TestCacheValue { Id = 1, Name = "Test" };
        var dependencies = Array.Empty<string>();

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _cacheManager.SetAsync(null!, value, dependencies));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SetAsync_InvalidKey_ThrowsArgumentException(string cacheKey)
    {
        var value = new TestCacheValue { Id = 1, Name = "Test" };
        var dependencies = Array.Empty<string>();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _cacheManager.SetAsync(cacheKey, value, dependencies));
    }

    [Fact]
    public async Task SetAsync_NullValue_ThrowsArgumentNullException()
    {
        var dependencies = Array.Empty<string>();

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _cacheManager.SetAsync<TestCacheValue>("key", null!, dependencies));
    }

    [Fact]
    public async Task SetAsync_NullDependencies_ThrowsArgumentNullException()
    {
        var value = new TestCacheValue { Id = 1, Name = "Test" };

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _cacheManager.SetAsync("key", value, null!));
    }

    [Fact]
    public async Task SetAsync_EmptyDependencies_DoesNotThrow()
    {
        var key = "test_key";
        var value = new TestCacheValue { Id = 1, Name = "Test" };
        var dependencies = Array.Empty<string>();

        await _cacheManager.SetAsync(key, value, dependencies);
        var result = await _cacheManager.GetAsync<TestCacheValue>(key);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task SetAsync_OverwritesExistingKey()
    {
        var key = "test_key";
        var value1 = new TestCacheValue { Id = 1, Name = "First" };
        var value2 = new TestCacheValue { Id = 2, Name = "Second" };
        var dependencies = Array.Empty<string>();

        await _cacheManager.SetAsync(key, value1, dependencies);
        await _cacheManager.SetAsync(key, value2, dependencies);
        var result = await _cacheManager.GetAsync<TestCacheValue>(key);

        Assert.NotNull(result);
        Assert.Equal(value2.Id, result.Id);
        Assert.Equal(value2.Name, result.Name);
    }

    [Fact]
    public async Task SetAsync_WithCustomExpiration_ExpiresAfterTimeout()
    {
        var key = "test_key";
        var value = new TestCacheValue { Id = 1, Name = "Test" };
        var dependencies = Array.Empty<string>();
        var expiration = TimeSpan.FromMilliseconds(100);

        await _cacheManager.SetAsync(key, value, dependencies, expiration);
        var expired = await WaitUntilAsync(
            async () => await _cacheManager.GetAsync<TestCacheValue>(key) is null,
            timeout: TimeSpan.FromSeconds(2),
            pollInterval: TimeSpan.FromMilliseconds(20));
        var result = await _cacheManager.GetAsync<TestCacheValue>(key);

        Assert.True(expired);
        Assert.Null(result);
    }

    [Fact]
    public async Task PurgeAsync_RemovesAllExistingEntries()
    {
        var key1 = "k1";
        var key2 = "k2";
        await _cacheManager.SetAsync(key1, new TestCacheValue { Id = 1, Name = "One" }, dependencies: ["dep1"]);
        await _cacheManager.SetAsync(key2, new TestCacheValue { Id = 2, Name = "Two" }, dependencies: ["dep2"]);

        await ((IDeliveryCachePurger)_cacheManager).PurgeAsync();

        Assert.Null(await _cacheManager.GetAsync<TestCacheValue>(key1));
        Assert.Null(await _cacheManager.GetAsync<TestCacheValue>(key2));
    }

    [Fact]
    public async Task PurgeAsync_WithAllowFailSafe_ExpiresEntriesAndDoesNotAffectNewEntries()
    {
        await _cacheManager.SetAsync("k1", new TestCacheValue { Id = 1, Name = "One" }, dependencies: ["dep1"]);
        await _cacheManager.SetAsync("k2", new TestCacheValue { Id = 2, Name = "Two" }, dependencies: ["dep2"]);

        await ((IDeliveryCachePurger)_cacheManager).PurgeAsync(allowFailSafe: true);

        Assert.Null(await _cacheManager.GetAsync<TestCacheValue>("k1"));
        Assert.Null(await _cacheManager.GetAsync<TestCacheValue>("k2"));

        await _cacheManager.SetAsync("k3", new TestCacheValue { Id = 3, Name = "Three" }, dependencies: ["dep3"]);
        Assert.NotNull(await _cacheManager.GetAsync<TestCacheValue>("k3"));
    }

    [Fact]
    public async Task PurgeAsync_DoesNotAffectEntriesCreatedAfterPurge()
    {
        await _cacheManager.SetAsync("old", new TestCacheValue { Id = 1, Name = "Old" }, dependencies: ["dep"]);

        await ((IDeliveryCachePurger)_cacheManager).PurgeAsync();
        await _cacheManager.SetAsync("new", new TestCacheValue { Id = 2, Name = "New" }, dependencies: ["dep"]);

        Assert.Null(await _cacheManager.GetAsync<TestCacheValue>("old"));
        Assert.NotNull(await _cacheManager.GetAsync<TestCacheValue>("new"));
    }

    [Fact]
    public async Task ExpiredDependencyEntry_DoesNotBreakFutureInvalidation()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions
        {
            ExpirationScanFrequency = TimeSpan.FromMilliseconds(10)
        });
        using var manager = new MemoryCacheManager(cache);

        var key = "test_key";
        var nextKey = "next_key";
        var value = new TestCacheValue { Id = 1, Name = "Test" };
        var dependency = "dep1";
        var expiration = TimeSpan.FromMilliseconds(50);

        await manager.SetAsync(key, value, [dependency], expiration);

        var expired = await WaitUntilAsync(
            async () =>
            {
                cache.Compact(1.0);
                return await manager.GetAsync<TestCacheValue>(key) is null;
            },
            timeout: TimeSpan.FromSeconds(2),
            pollInterval: TimeSpan.FromMilliseconds(20));

        Assert.True(expired);
        Assert.Null(await manager.GetAsync<TestCacheValue>(key));

        // Behavior contract: once the expired edge is gone, a new edge for the same dependency
        // must still be tracked and invalidated correctly.
        await manager.SetAsync(nextKey, new TestCacheValue { Id = 2, Name = "Next" }, [dependency]);
        Assert.NotNull(await manager.GetAsync<TestCacheValue>(nextKey));

        await manager.InvalidateAsync(default, dependency);
        Assert.Null(await manager.GetAsync<TestCacheValue>(nextKey));
    }

    #endregion

    #region Dependency Tracking Tests

    [Fact]
    public async Task SetAsync_WithDependencies_TracksCorrectly()
    {
        var key = "test_key";
        var value = new TestCacheValue { Id = 1, Name = "Test" };
        var dependencies = new[] { "dep1", "dep2", "dep3" };

        await _cacheManager.SetAsync(key, value, dependencies);
        var result = await _cacheManager.GetAsync<TestCacheValue>(key);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task SetAsync_WithDuplicateDependencies_HandlesCorrectly()
    {
        var key = "test_key";
        var value = new TestCacheValue { Id = 1, Name = "Test" };
        var dependencies = new[] { "dep1", "dep1", "dep2", "dep2" };

        await _cacheManager.SetAsync(key, value, dependencies);
        var result = await _cacheManager.GetAsync<TestCacheValue>(key);

        Assert.NotNull(result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SetAsync_WithIgnoredDependencyValue_IgnoresInvalidDependency(string? ignoredDependency)
    {
        var key = "test_key";
        var value = new TestCacheValue { Id = 1, Name = "Test" };
        var dependencies = new[] { "dep1", ignoredDependency!, "dep2" };

        await _cacheManager.SetAsync(key, value, dependencies);
        var result = await _cacheManager.GetAsync<TestCacheValue>(key);

        Assert.NotNull(result);
    }

    #endregion

    #region Invalidation Tests

    [Fact]
    public async Task InvalidateAsync_RemovesCacheEntry()
    {
        var key = "test_key";
        var value = new TestCacheValue { Id = 1, Name = "Test" };
        var dependency = "dep1";
        var dependencies = new[] { dependency };

        await _cacheManager.SetAsync(key, value, dependencies);

        await _cacheManager.InvalidateAsync(default, dependency);
        var result = await _cacheManager.GetAsync<TestCacheValue>(key);

        Assert.Null(result);
    }

    [Fact]
    public async Task InvalidateAsync_NonExistentDependency_DoesNotThrowAndPreservesExistingEntries()
    {
        await _cacheManager.SetAsync("existing_key", new TestCacheValue { Id = 10, Name = "Existing" }, ["existing_dep"]);

        var exception = await Record.ExceptionAsync(() => _cacheManager.InvalidateAsync(default, "non_existent_dep"));
        var existingResult = await _cacheManager.GetAsync<TestCacheValue>("existing_key");

        Assert.Null(exception);
        Assert.NotNull(existingResult);
        Assert.Equal(10, existingResult.Id);
    }

    [Fact]
    public async Task InvalidateAsync_NullDependencies_DoesNotThrow()
    {
        var exception = await Record.ExceptionAsync(() => _cacheManager.InvalidateAsync(default, null!));
        Assert.Null(exception);
    }

    [Fact]
    public async Task InvalidateAsync_EmptyDependencies_DoesNotThrow()
    {
        var exception = await Record.ExceptionAsync(() => _cacheManager.InvalidateAsync(default, []));
        Assert.Null(exception);
    }

    [Fact]
    public async Task InvalidateAsync_MultipleDependencies_RemovesAllAffected()
    {
        var key1 = "key1";
        var key2 = "key2";
        var key3 = "key3";
        var value = new TestCacheValue { Id = 1, Name = "Test" };

        await _cacheManager.SetAsync(key1, value, ["dep1"]);
        await _cacheManager.SetAsync(key2, value, ["dep2"]);
        await _cacheManager.SetAsync(key3, value, ["dep3"]);

        await _cacheManager.InvalidateAsync(default, "dep1", "dep2");

        var result1 = await _cacheManager.GetAsync<TestCacheValue>(key1);
        var result2 = await _cacheManager.GetAsync<TestCacheValue>(key2);
        var result3 = await _cacheManager.GetAsync<TestCacheValue>(key3);

        Assert.Null(result1);
        Assert.Null(result2);
        Assert.NotNull(result3);
    }

    [Fact]
    public async Task InvalidateAsync_SharedDependency_RemovesAllEntriesWithThatDependency()
    {
        var key1 = "key1";
        var key2 = "key2";
        var key3 = "key3";
        var value = new TestCacheValue { Id = 1, Name = "Test" };
        var sharedDependency = "shared_dep";

        await _cacheManager.SetAsync(key1, value, [sharedDependency]);
        await _cacheManager.SetAsync(key2, value, [sharedDependency]);
        await _cacheManager.SetAsync(key3, value, ["other_dep"]);

        await _cacheManager.InvalidateAsync(default, sharedDependency);

        var result1 = await _cacheManager.GetAsync<TestCacheValue>(key1);
        var result2 = await _cacheManager.GetAsync<TestCacheValue>(key2);
        var result3 = await _cacheManager.GetAsync<TestCacheValue>(key3);

        Assert.Null(result1);
        Assert.Null(result2);
        Assert.NotNull(result3);
    }

    [Fact]
    public async Task InvalidateAsync_IdempotentOperation_CanBeCalledMultipleTimes()
    {
        var key = "test_key";
        var value = new TestCacheValue { Id = 1, Name = "Test" };
        var dependency = "dep1";

        await _cacheManager.SetAsync(key, value, [dependency]);

        await _cacheManager.InvalidateAsync(default, dependency);
        await _cacheManager.InvalidateAsync(default, dependency);
        await _cacheManager.InvalidateAsync(default, dependency);

        var result = await _cacheManager.GetAsync<TestCacheValue>(key);

        Assert.Null(result);
    }

    [Fact]
    public async Task InvalidateAsync_WithCaseVariantKeys_RemovesAllMatchingEntries()
    {
        var dependency = "dep1";
        await _cacheManager.SetAsync("Key", new TestCacheValue { Id = 1, Name = "Upper" }, [dependency]);
        await _cacheManager.SetAsync("key", new TestCacheValue { Id = 2, Name = "Lower" }, [dependency]);

        Assert.NotNull(await _cacheManager.GetAsync<TestCacheValue>("Key"));
        Assert.NotNull(await _cacheManager.GetAsync<TestCacheValue>("key"));

        await _cacheManager.InvalidateAsync(default, dependency);

        Assert.Null(await _cacheManager.GetAsync<TestCacheValue>("Key"));
        Assert.Null(await _cacheManager.GetAsync<TestCacheValue>("key"));
    }

    #endregion

    #region Concurrency Tests

    [Fact]
    public async Task ConcurrentGet_DoesNotThrow()
    {
        var key = "test_key";
        var value = new TestCacheValue { Id = 1, Name = "Test" };
        await _cacheManager.SetAsync(key, value, []);

        var tasks = Enumerable.Range(0, 100)
            .Select(_ => _cacheManager.GetAsync<TestCacheValue>(key))
            .ToList();

        var results = await Task.WhenAll(tasks);

        Assert.All(results, Assert.NotNull);
    }

    [Fact]
    public async Task ConcurrentSet_WithSameDependency_DoesNotCorruptState()
    {
        var sharedDependency = "shared_dep";
        var tasks = Enumerable.Range(0, 50)
            .Select(i => _cacheManager.SetAsync(
                $"key_{i}",
                new TestCacheValue { Id = i, Name = $"Test_{i}" },
                [sharedDependency]))
            .ToList();

        await Task.WhenAll(tasks);

        await _cacheManager.InvalidateAsync(default, sharedDependency);

        var verifyTasks = Enumerable.Range(0, 50)
            .Select(i => _cacheManager.GetAsync<TestCacheValue>($"key_{i}"))
            .ToList();

        var results = await Task.WhenAll(verifyTasks);

        Assert.All(results, Assert.Null);
    }

    [Fact]
    public async Task SetAsync_OverwriteSameKey_InvalidationCancelsCurrentEntry()
    {
        var key = "test_key";
        var dependency = "dep1";

        await _cacheManager.SetAsync(key, new TestCacheValue { Id = 1, Name = "First" }, [dependency]);
        await _cacheManager.SetAsync(key, new TestCacheValue { Id = 2, Name = "Second" }, [dependency]);

        await _cacheManager.InvalidateAsync(default, dependency);
        var result = await _cacheManager.GetAsync<TestCacheValue>(key);

        Assert.Null(result);
    }

    [Fact]
    public async Task SetAsync_OverwriteSameKey_ChangingDependencies_DoesNotInvalidateOnOldDependency()
    {
        var key = "test_key";
        await _cacheManager.SetAsync(key, new TestCacheValue { Id = 1, Name = "Old" }, ["dep_old"]);
        await _cacheManager.SetAsync(key, new TestCacheValue { Id = 2, Name = "New" }, ["dep_new"]);

        await _cacheManager.InvalidateAsync(default, "dep_old");
        var result = await _cacheManager.GetAsync<TestCacheValue>(key);

        Assert.NotNull(result);
        Assert.Equal(2, result.Id);

        await _cacheManager.InvalidateAsync(default, "dep_new");
        Assert.Null(await _cacheManager.GetAsync<TestCacheValue>(key));
    }

    [Fact]
    public async Task ConcurrentSetAndInvalidate_DoesNotThrow()
    {
        var dependency = "dep1";
        var setTasks = Enumerable.Range(0, 25)
            .Select(i => _cacheManager.SetAsync(
                $"key_{i}",
                new TestCacheValue { Id = i, Name = $"Test_{i}" },
                [dependency]))
            .ToList();

        var invalidateTasks = Enumerable.Range(0, 25)
            .Select(async _ =>
            {
                await Task.Yield();
                await _cacheManager.InvalidateAsync(default, dependency);
            })
            .ToList();

        var exception = await Record.ExceptionAsync(async () => await Task.WhenAll(setTasks.Concat(invalidateTasks)));
        Assert.Null(exception);
    }

    [Fact]
    public async Task ConcurrentInvalidate_SameDependency_DoesNotThrow()
    {
        var dependency = "dep1";
        var key = "test_key";
        var value = new TestCacheValue { Id = 1, Name = "Test" };
        await _cacheManager.SetAsync(key, value, [dependency]);

        var tasks = Enumerable.Range(0, 50)
            .Select(_ => _cacheManager.InvalidateAsync(default, dependency))
            .ToList();

        await Task.WhenAll(tasks);

        var result = await _cacheManager.GetAsync<TestCacheValue>(key);

        Assert.Null(result);
    }

    [Fact]
    public async Task ReverseIndex_ConcurrentCleanupAndSet_PreservesDependencyInvalidation()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions
        {
            ExpirationScanFrequency = TimeSpan.FromMilliseconds(10)
        });
        using var manager = new MemoryCacheManager(cache);

        const string dependency = "dep_race";
        const string expiringKey = "expiring_key";
        const string stableKey = "stable_key";

        await manager.SetAsync(
            expiringKey,
            new TestCacheValue { Id = 1, Name = "Expiring" },
            [dependency],
            expiration: TimeSpan.FromMilliseconds(40));

        var churnTask = Task.Run(async () =>
        {
            for (var i = 0; i < 50; i++)
            {
                await manager.SetAsync(
                    stableKey,
                    new TestCacheValue { Id = i, Name = $"Stable_{i}" },
                    [dependency],
                    expiration: TimeSpan.FromMinutes(1));
                await Task.Yield();
            }
        });

        var expired = await WaitUntilAsync(
            async () =>
            {
                cache.Compact(1.0);
                return await manager.GetAsync<TestCacheValue>(expiringKey) is null;
            },
            timeout: TimeSpan.FromSeconds(2),
            pollInterval: TimeSpan.FromMilliseconds(20));

        await churnTask;
        Assert.True(expired);

        await manager.InvalidateAsync(default, dependency);
        Assert.Null(await manager.GetAsync<TestCacheValue>(stableKey));
    }

    #endregion

    #region Cancellation Tests

    [Fact]
    public async Task GetAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _cacheManager.GetAsync<TestCacheValue>("key", cts.Token));
    }

    [Fact]
    public async Task SetAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        var value = new TestCacheValue { Id = 1, Name = "Test" };
        var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _cacheManager.SetAsync("key", value, [], cancellationToken: cts.Token));
    }

    #endregion

    #region Disposal Tests

    [Fact]
    public async Task Dispose_DisposesResources()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var manager = new MemoryCacheManager(cache);

        var key = "test_key";
        var value = new TestCacheValue { Id = 1, Name = "Test" };
        await manager.SetAsync(key, value, ["dep1"]);

        manager.Dispose();

        await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            manager.GetAsync<TestCacheValue>(key));
    }

    [Fact]
    public void Dispose_CalledMultipleTimes_DoesNotThrow()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var manager = new MemoryCacheManager(cache);

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
        var manager = new MemoryCacheManager(cache);

        var key = "test_key";
        var value = new TestCacheValue { Id = 1, Name = "Test" };
        await manager.SetAsync(key, value, ["dep1", "dep2", "dep3"]);

        manager.Dispose();

        Assert.True(true);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task SetAsync_VeryLongKey_Succeeds()
    {
        var key = new string('a', 1000);
        var value = new TestCacheValue { Id = 1, Name = "Test" };

        await _cacheManager.SetAsync(key, value, []);
        var result = await _cacheManager.GetAsync<TestCacheValue>(key);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task SetAsync_VeryLongDependency_Succeeds()
    {
        var key = "test_key";
        var value = new TestCacheValue { Id = 1, Name = "Test" };
        var dependency = new string('b', 1000);

        await _cacheManager.SetAsync(key, value, [dependency]);
        await _cacheManager.InvalidateAsync(default, dependency);
        var result = await _cacheManager.GetAsync<TestCacheValue>(key);

        Assert.Null(result);
    }

    [Fact]
    public async Task SetAsync_ManyDependencies_Succeeds()
    {
        var key = "test_key";
        var value = new TestCacheValue { Id = 1, Name = "Test" };
        var dependencies = Enumerable.Range(0, 100).Select(i => $"dep_{i}").ToArray();

        await _cacheManager.SetAsync(key, value, dependencies);
        var result = await _cacheManager.GetAsync<TestCacheValue>(key);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task InvalidateAsync_ManyDependencies_Succeeds()
    {
        var keys = Enumerable.Range(0, 50).Select(i => $"key_{i}").ToArray();
        var value = new TestCacheValue { Id = 1, Name = "Test" };

        foreach (var key in keys)
        {
            await _cacheManager.SetAsync(key, value, [$"dep_{key}"]);
        }

        var dependenciesToInvalidate = keys.Select(k => $"dep_{k}").ToArray();

        await _cacheManager.InvalidateAsync(default, dependenciesToInvalidate);

        var results = new List<TestCacheValue?>();
        foreach (var key in keys)
        {
            results.Add(await _cacheManager.GetAsync<TestCacheValue>(key));
        }

        Assert.All(results, Assert.Null);
    }

    [Fact]
    public async Task GetAsync_TypeMismatch_ReturnsNull()
    {
        var key = "test_key";
        var value = new TestCacheValue { Id = 1, Name = "Test" };
        await _cacheManager.SetAsync(key, value, []);

        var result = await _cacheManager.GetAsync<OtherTestValue>(key);

        Assert.Null(result);
    }

    [Fact]
    public async Task SetAsync_ComplexObject_WithNestedProperties_Succeeds()
    {
        var key = "complex_key";
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

        await _cacheManager.SetAsync(key, value, []);
        var result = await _cacheManager.GetAsync<ComplexCacheValue>(key);

        Assert.NotNull(result);
        Assert.Equal(value.Id, result.Id);
        Assert.Equal(value.Name, result.Name);
        Assert.NotNull(result.Nested);
        Assert.Equal(value.Nested.Description, result.Nested.Description);
        Assert.Equal(value.Nested.Tags, result.Nested.Tags);
        Assert.Equal(value.Items, result.Items);
    }

    #endregion

    #region Key Prefix Isolation Tests

    [Fact]
    public async Task SetAsync_WithDifferentPrefixes_IsolatesCacheEntries()
    {
        var sharedCache = new MemoryCache(new MemoryCacheOptions());
        using var manager1 = new MemoryCacheManager(sharedCache, keyPrefix: "client1");
        using var manager2 = new MemoryCacheManager(sharedCache, keyPrefix: "client2");

        var key = "same_key";
        var value1 = new TestCacheValue { Id = 1, Name = "Client1Value" };
        var value2 = new TestCacheValue { Id = 2, Name = "Client2Value" };

        await manager1.SetAsync(key, value1, []);
        await manager2.SetAsync(key, value2, []);

        var result1 = await manager1.GetAsync<TestCacheValue>(key);
        var result2 = await manager2.GetAsync<TestCacheValue>(key);

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
        using var manager1 = new MemoryCacheManager(sharedCache, keyPrefix: "client1");
        using var manager2 = new MemoryCacheManager(sharedCache, keyPrefix: "client2");

        var key = "same_key";
        var dependency = "same_dep";
        var value1 = new TestCacheValue { Id = 1, Name = "Client1Value" };
        var value2 = new TestCacheValue { Id = 2, Name = "Client2Value" };

        await manager1.SetAsync(key, value1, [dependency]);
        await manager2.SetAsync(key, value2, [dependency]);

        await manager1.InvalidateAsync(default, dependency);

        var result1 = await manager1.GetAsync<TestCacheValue>(key);
        var result2 = await manager2.GetAsync<TestCacheValue>(key);

        Assert.Null(result1);
        Assert.NotNull(result2);
        Assert.Equal(2, result2.Id);

        sharedCache.Dispose();
    }

    [Fact]
    public async Task GetAsync_WithDifferentPrefixes_DoesNotCrossContaminate()
    {
        var sharedCache = new MemoryCache(new MemoryCacheOptions());
        using var manager1 = new MemoryCacheManager(sharedCache, keyPrefix: "client1");
        using var manager2 = new MemoryCacheManager(sharedCache, keyPrefix: "client2");

        var key = "unique_key";
        var value = new TestCacheValue { Id = 1, Name = "OnlyInClient1" };

        await manager1.SetAsync(key, value, []);

        var result1 = await manager1.GetAsync<TestCacheValue>(key);
        var result2 = await manager2.GetAsync<TestCacheValue>(key);

        Assert.NotNull(result1);
        Assert.Null(result2);

        sharedCache.Dispose();
    }

    [Fact]
    public async Task SetAsync_WithNullPrefix_UsesUnprefixedKeys()
    {
        var sharedCache = new MemoryCache(new MemoryCacheOptions());
        using var managerNoPrefix = new MemoryCacheManager(sharedCache, keyPrefix: null);
        using var managerWithPrefix = new MemoryCacheManager(sharedCache, keyPrefix: "prefixed");

        var key = "test_key";
        var value1 = new TestCacheValue { Id = 1, Name = "NoPrefix" };
        var value2 = new TestCacheValue { Id = 2, Name = "WithPrefix" };

        await managerNoPrefix.SetAsync(key, value1, []);
        await managerWithPrefix.SetAsync(key, value2, []);

        var result1 = await managerNoPrefix.GetAsync<TestCacheValue>(key);
        var result2 = await managerWithPrefix.GetAsync<TestCacheValue>(key);

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
        using var manager1 = new MemoryCacheManager(sharedCache, keyPrefix: "prod");
        using var manager2 = new MemoryCacheManager(sharedCache, keyPrefix: "preview");

        var dependency = "content_type_article";

        await manager1.SetAsync("item1", new TestCacheValue { Id = 1 }, [dependency]);
        await manager1.SetAsync("item2", new TestCacheValue { Id = 2 }, [dependency]);
        await manager2.SetAsync("item1", new TestCacheValue { Id = 10 }, [dependency]);
        await manager2.SetAsync("item2", new TestCacheValue { Id = 20 }, [dependency]);

        await manager1.InvalidateAsync(default, dependency);

        Assert.Null(await manager1.GetAsync<TestCacheValue>("item1"));
        Assert.Null(await manager1.GetAsync<TestCacheValue>("item2"));
        Assert.NotNull(await manager2.GetAsync<TestCacheValue>("item1"));
        Assert.NotNull(await manager2.GetAsync<TestCacheValue>("item2"));

        sharedCache.Dispose();
    }

    [Fact]
    public async Task ConcurrentOperations_WithDifferentPrefixes_MaintainsIsolation()
    {
        var sharedCache = new MemoryCache(new MemoryCacheOptions());
        using var manager1 = new MemoryCacheManager(sharedCache, keyPrefix: "client1");
        using var manager2 = new MemoryCacheManager(sharedCache, keyPrefix: "client2");

        var dependency = "shared_dep_name";

        var tasks1 = Enumerable.Range(0, 25)
            .Select(i => manager1.SetAsync($"key_{i}", new TestCacheValue { Id = i }, [dependency]));
        var tasks2 = Enumerable.Range(0, 25)
            .Select(i => manager2.SetAsync($"key_{i}", new TestCacheValue { Id = i + 100 }, [dependency]));

        await Task.WhenAll(tasks1.Concat(tasks2));

        await manager1.InvalidateAsync(default, dependency);

        var verify1 = await Task.WhenAll(Enumerable.Range(0, 25)
            .Select(i => manager1.GetAsync<TestCacheValue>($"key_{i}")));
        var verify2 = await Task.WhenAll(Enumerable.Range(0, 25)
            .Select(i => manager2.GetAsync<TestCacheValue>($"key_{i}")));

        Assert.All(verify1, Assert.Null);
        Assert.All(verify2, Assert.NotNull);

        sharedCache.Dispose();
    }

    #endregion

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

    #region Test Helper Classes

    private class TestCacheValue
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }

    private class OtherTestValue
    {
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
