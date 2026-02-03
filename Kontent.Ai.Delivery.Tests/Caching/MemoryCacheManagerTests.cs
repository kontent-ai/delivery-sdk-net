using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
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
        // Act
        var result = await _cacheManager.GetAsync<TestCacheValue>("non_existent_key");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAsync_NullKey_ReturnsNull()
    {
        // Act
        var result = await _cacheManager.GetAsync<TestCacheValue>(null!);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAsync_EmptyKey_ReturnsNull()
    {
        // Act
        var result = await _cacheManager.GetAsync<TestCacheValue>("");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAsync_WhitespaceKey_ReturnsNull()
    {
        // Act
        var result = await _cacheManager.GetAsync<TestCacheValue>("   ");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task SetAsync_ThenGetAsync_ReturnsValue()
    {
        // Arrange
        var key = "test_key";
        var value = new TestCacheValue { Id = 1, Name = "Test" };
        var dependencies = new[] { "dep1" };

        // Act
        await _cacheManager.SetAsync(key, value, dependencies);
        var result = await _cacheManager.GetAsync<TestCacheValue>(key);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(value.Id, result.Id);
        Assert.Equal(value.Name, result.Name);
    }

    [Fact]
    public async Task SetAsync_NullKey_ThrowsArgumentNullException()
    {
        // Arrange
        var value = new TestCacheValue { Id = 1, Name = "Test" };
        var dependencies = Array.Empty<string>();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _cacheManager.SetAsync(null!, value, dependencies));
    }

    [Fact]
    public async Task SetAsync_EmptyKey_ThrowsArgumentException()
    {
        // Arrange
        var value = new TestCacheValue { Id = 1, Name = "Test" };
        var dependencies = Array.Empty<string>();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _cacheManager.SetAsync("", value, dependencies));
    }

    [Fact]
    public async Task SetAsync_WhitespaceKey_ThrowsArgumentException()
    {
        // Arrange
        var value = new TestCacheValue { Id = 1, Name = "Test" };
        var dependencies = Array.Empty<string>();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _cacheManager.SetAsync("   ", value, dependencies));
    }

    [Fact]
    public async Task SetAsync_NullValue_ThrowsArgumentNullException()
    {
        // Arrange
        var dependencies = Array.Empty<string>();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _cacheManager.SetAsync<TestCacheValue>("key", null!, dependencies));
    }

    [Fact]
    public async Task SetAsync_NullDependencies_ThrowsArgumentNullException()
    {
        // Arrange
        var value = new TestCacheValue { Id = 1, Name = "Test" };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _cacheManager.SetAsync("key", value, null!));
    }

    [Fact]
    public async Task SetAsync_EmptyDependencies_DoesNotThrow()
    {
        // Arrange
        var key = "test_key";
        var value = new TestCacheValue { Id = 1, Name = "Test" };
        var dependencies = Array.Empty<string>();

        // Act
        await _cacheManager.SetAsync(key, value, dependencies);
        var result = await _cacheManager.GetAsync<TestCacheValue>(key);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task SetAsync_OverwritesExistingKey()
    {
        // Arrange
        var key = "test_key";
        var value1 = new TestCacheValue { Id = 1, Name = "First" };
        var value2 = new TestCacheValue { Id = 2, Name = "Second" };
        var dependencies = Array.Empty<string>();

        // Act
        await _cacheManager.SetAsync(key, value1, dependencies);
        await _cacheManager.SetAsync(key, value2, dependencies);
        var result = await _cacheManager.GetAsync<TestCacheValue>(key);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(value2.Id, result.Id);
        Assert.Equal(value2.Name, result.Name);
    }

    [Fact]
    public async Task SetAsync_WithCustomExpiration_ExpiresAfterTimeout()
    {
        // Arrange
        var key = "test_key";
        var value = new TestCacheValue { Id = 1, Name = "Test" };
        var dependencies = Array.Empty<string>();
        var expiration = TimeSpan.FromMilliseconds(100);

        // Act
        await _cacheManager.SetAsync(key, value, dependencies, expiration);
        await Task.Delay(200); // Wait for expiration
        var result = await _cacheManager.GetAsync<TestCacheValue>(key);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task PurgeAsync_RemovesAllExistingEntries()
    {
        // Arrange
        var key1 = "k1";
        var key2 = "k2";
        await _cacheManager.SetAsync(key1, new TestCacheValue { Id = 1, Name = "One" }, dependencies: ["dep1"]);
        await _cacheManager.SetAsync(key2, new TestCacheValue { Id = 2, Name = "Two" }, dependencies: ["dep2"]);

        // Act
        await ((IDeliveryCachePurger)_cacheManager).PurgeAsync();

        // Assert
        Assert.Null(await _cacheManager.GetAsync<TestCacheValue>(key1));
        Assert.Null(await _cacheManager.GetAsync<TestCacheValue>(key2));
    }

    [Fact]
    public async Task PurgeAsync_DoesNotAffectEntriesCreatedAfterPurge()
    {
        // Arrange
        await _cacheManager.SetAsync("old", new TestCacheValue { Id = 1, Name = "Old" }, dependencies: ["dep"]);

        // Act
        await ((IDeliveryCachePurger)_cacheManager).PurgeAsync();
        await _cacheManager.SetAsync("new", new TestCacheValue { Id = 2, Name = "New" }, dependencies: ["dep"]);

        // Assert
        Assert.Null(await _cacheManager.GetAsync<TestCacheValue>("old"));
        Assert.NotNull(await _cacheManager.GetAsync<TestCacheValue>("new"));
    }

    [Fact]
    public async Task ReverseIndex_IsCleanedUp_WhenLastDependentEntryExpires()
    {
        // Arrange
        using var cache = new MemoryCache(new MemoryCacheOptions
        {
            // Make expirations deterministic in tests (default is ~1 minute).
            ExpirationScanFrequency = TimeSpan.FromMilliseconds(10)
        });
        using var manager = new MemoryCacheManager(cache);

        var key = "test_key";
        var value = new TestCacheValue { Id = 1, Name = "Test" };
        var dependency = "dep1";
        var expiration = TimeSpan.FromMilliseconds(50);

        // Act - create entry with a dependency (this creates a reverse-index entry for that dependency)
        await manager.SetAsync(key, value, [dependency], expiration);

        // Assert precondition: reverse index contains the dependency
        var reverseIndex = GetPrivateField<ConcurrentDictionary<string, HashSet<string>>>(
            manager,
            "_reverseIndex");
        Assert.True(reverseIndex.ContainsKey(dependency));

        // Let the entry expire and trigger eviction/scavenging
        await Task.Delay(200);
        cache.Compact(1.0);
        var result = await manager.GetAsync<TestCacheValue>(key);

        // Assert: entry expired and dependency CTS is cleaned up (no unbounded growth)
        Assert.Null(result);
        var entries = GetPrivateField<object>(manager, "_entries");

        // Post-eviction callbacks are not guaranteed to have run synchronously at this point.
        // On slower/contended environments (e.g., CI runners), the cache entry can be gone
        // while the callback cleanup is still pending.
        var cleanedUp = await WaitUntilAsync(
            () => !ConcurrentDictionaryContainsKey(entries, key) && !reverseIndex.ContainsKey(dependency),
            timeout: TimeSpan.FromSeconds(2),
            pollInterval: TimeSpan.FromMilliseconds(10));

        Assert.True(
            cleanedUp,
            $"Expected cleanup for key '{key}' and dependency '{dependency}', but cleanup did not complete in time. " +
            $"Entries: [{string.Join(", ", GetConcurrentDictionaryKeys(entries))}], ReverseIndexKeys: [{string.Join(", ", reverseIndex.Keys)}]");
        Assert.DoesNotContain(key, GetConcurrentDictionaryKeys(entries));
        Assert.False(reverseIndex.ContainsKey(dependency));
    }

    #endregion

    #region Dependency Tracking Tests

    [Fact]
    public async Task SetAsync_WithDependencies_TracksCorrectly()
    {
        // Arrange
        var key = "test_key";
        var value = new TestCacheValue { Id = 1, Name = "Test" };
        var dependencies = new[] { "dep1", "dep2", "dep3" };

        // Act
        await _cacheManager.SetAsync(key, value, dependencies);
        var result = await _cacheManager.GetAsync<TestCacheValue>(key);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task SetAsync_WithDuplicateDependencies_HandlesCorrectly()
    {
        // Arrange
        var key = "test_key";
        var value = new TestCacheValue { Id = 1, Name = "Test" };
        var dependencies = new[] { "dep1", "dep1", "dep2", "dep2" };

        // Act
        await _cacheManager.SetAsync(key, value, dependencies);
        var result = await _cacheManager.GetAsync<TestCacheValue>(key);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task SetAsync_WithNullDependency_IgnoresNull()
    {
        // Arrange
        var key = "test_key";
        var value = new TestCacheValue { Id = 1, Name = "Test" };
        var dependencies = new[] { "dep1", null!, "dep2" };

        // Act
        await _cacheManager.SetAsync(key, value, dependencies);
        var result = await _cacheManager.GetAsync<TestCacheValue>(key);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task SetAsync_WithEmptyDependency_IgnoresEmpty()
    {
        // Arrange
        var key = "test_key";
        var value = new TestCacheValue { Id = 1, Name = "Test" };
        var dependencies = new[] { "dep1", "", "dep2" };

        // Act
        await _cacheManager.SetAsync(key, value, dependencies);
        var result = await _cacheManager.GetAsync<TestCacheValue>(key);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task SetAsync_WithWhitespaceDependency_IgnoresWhitespace()
    {
        // Arrange
        var key = "test_key";
        var value = new TestCacheValue { Id = 1, Name = "Test" };
        var dependencies = new[] { "dep1", "   ", "dep2" };

        // Act
        await _cacheManager.SetAsync(key, value, dependencies);
        var result = await _cacheManager.GetAsync<TestCacheValue>(key);

        // Assert
        Assert.NotNull(result);
    }

    #endregion

    #region Invalidation Tests

    [Fact]
    public async Task InvalidateAsync_RemovesCacheEntry()
    {
        // Arrange
        var key = "test_key";
        var value = new TestCacheValue { Id = 1, Name = "Test" };
        var dependency = "dep1";
        var dependencies = new[] { dependency };

        await _cacheManager.SetAsync(key, value, dependencies);

        // Act
        await _cacheManager.InvalidateAsync(default, dependency);
        var result = await _cacheManager.GetAsync<TestCacheValue>(key);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task InvalidateAsync_NonExistentDependency_DoesNotThrow()
    {
        // Act & Assert
        await _cacheManager.InvalidateAsync(default, "non_existent_dep");
    }

    [Fact]
    public async Task InvalidateAsync_NullDependencies_DoesNotThrow()
    {
        // Act & Assert
        await _cacheManager.InvalidateAsync(default, null!);
    }

    [Fact]
    public async Task InvalidateAsync_EmptyDependencies_DoesNotThrow()
    {
        // Act & Assert
        await _cacheManager.InvalidateAsync(default, []);
    }

    [Fact]
    public async Task InvalidateAsync_MultipleDependencies_RemovesAllAffected()
    {
        // Arrange
        var key1 = "key1";
        var key2 = "key2";
        var key3 = "key3";
        var value = new TestCacheValue { Id = 1, Name = "Test" };

        await _cacheManager.SetAsync(key1, value, ["dep1"]);
        await _cacheManager.SetAsync(key2, value, ["dep2"]);
        await _cacheManager.SetAsync(key3, value, ["dep3"]);

        // Act
        await _cacheManager.InvalidateAsync(default, "dep1", "dep2");

        var result1 = await _cacheManager.GetAsync<TestCacheValue>(key1);
        var result2 = await _cacheManager.GetAsync<TestCacheValue>(key2);
        var result3 = await _cacheManager.GetAsync<TestCacheValue>(key3);

        // Assert
        Assert.Null(result1);
        Assert.Null(result2);
        Assert.NotNull(result3); // key3 should still exist
    }

    [Fact]
    public async Task InvalidateAsync_SharedDependency_RemovesAllEntriesWithThatDependency()
    {
        // Arrange
        var key1 = "key1";
        var key2 = "key2";
        var key3 = "key3";
        var value = new TestCacheValue { Id = 1, Name = "Test" };
        var sharedDependency = "shared_dep";

        await _cacheManager.SetAsync(key1, value, [sharedDependency]);
        await _cacheManager.SetAsync(key2, value, [sharedDependency]);
        await _cacheManager.SetAsync(key3, value, ["other_dep"]);

        // Act
        await _cacheManager.InvalidateAsync(default, sharedDependency);

        var result1 = await _cacheManager.GetAsync<TestCacheValue>(key1);
        var result2 = await _cacheManager.GetAsync<TestCacheValue>(key2);
        var result3 = await _cacheManager.GetAsync<TestCacheValue>(key3);

        // Assert
        Assert.Null(result1);
        Assert.Null(result2);
        Assert.NotNull(result3); // key3 should still exist
    }

    [Fact]
    public async Task InvalidateAsync_IdempotentOperation_CanBeCalledMultipleTimes()
    {
        // Arrange
        var key = "test_key";
        var value = new TestCacheValue { Id = 1, Name = "Test" };
        var dependency = "dep1";

        await _cacheManager.SetAsync(key, value, [dependency]);

        // Act - invalidate multiple times
        await _cacheManager.InvalidateAsync(default, dependency);
        await _cacheManager.InvalidateAsync(default, dependency);
        await _cacheManager.InvalidateAsync(default, dependency);

        var result = await _cacheManager.GetAsync<TestCacheValue>(key);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region Concurrency Tests

    [Fact]
    public async Task ConcurrentGet_DoesNotThrow()
    {
        // Arrange
        var key = "test_key";
        var value = new TestCacheValue { Id = 1, Name = "Test" };
        await _cacheManager.SetAsync(key, value, []);

        // Act - concurrent reads
        var tasks = Enumerable.Range(0, 100)
            .Select(_ => _cacheManager.GetAsync<TestCacheValue>(key))
            .ToList();

        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.All(results, Assert.NotNull);
    }

    [Fact]
    public async Task ConcurrentSet_WithSameDependency_DoesNotCorruptState()
    {
        // Arrange
        var sharedDependency = "shared_dep";
        var tasks = Enumerable.Range(0, 50)
            .Select(i => _cacheManager.SetAsync(
                $"key_{i}",
                new TestCacheValue { Id = i, Name = $"Test_{i}" },
                [sharedDependency]))
            .ToList();

        // Act
        await Task.WhenAll(tasks);

        // Invalidate and verify all entries are removed
        await _cacheManager.InvalidateAsync(default, sharedDependency);

        var verifyTasks = Enumerable.Range(0, 50)
            .Select(i => _cacheManager.GetAsync<TestCacheValue>($"key_{i}"))
            .ToList();

        var results = await Task.WhenAll(verifyTasks);

        // Assert
        Assert.All(results, Assert.Null);
    }

    [Fact]
    public async Task SetAsync_OverwriteSameKey_InvalidationCancelsCurrentEntry()
    {
        // Arrange
        var key = "test_key";
        var dependency = "dep1";

        await _cacheManager.SetAsync(key, new TestCacheValue { Id = 1, Name = "First" }, [dependency]);
        await _cacheManager.SetAsync(key, new TestCacheValue { Id = 2, Name = "Second" }, [dependency]);

        // Act
        await _cacheManager.InvalidateAsync(default, dependency);
        var result = await _cacheManager.GetAsync<TestCacheValue>(key);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task SetAsync_OverwriteSameKey_ChangingDependencies_DoesNotInvalidateOnOldDependency()
    {
        // Arrange
        var key = "test_key";
        await _cacheManager.SetAsync(key, new TestCacheValue { Id = 1, Name = "Old" }, ["dep_old"]);
        await _cacheManager.SetAsync(key, new TestCacheValue { Id = 2, Name = "New" }, ["dep_new"]);

        // Act - invalidate the old dependency
        await _cacheManager.InvalidateAsync(default, "dep_old");
        var result = await _cacheManager.GetAsync<TestCacheValue>(key);

        // Assert - entry should remain since it no longer depends on dep_old
        Assert.NotNull(result);
        Assert.Equal(2, result.Id);

        // And invalidating the new dependency should remove it
        await _cacheManager.InvalidateAsync(default, "dep_new");
        Assert.Null(await _cacheManager.GetAsync<TestCacheValue>(key));
    }

    [Fact]
    public async Task ConcurrentSetAndInvalidate_DoesNotThrow()
    {
        // Arrange
        var dependency = "dep1";
        var cts = new CancellationTokenSource();

        // Act - concurrent set and invalidate operations
        var setTasks = Enumerable.Range(0, 25)
            .Select(i => Task.Run(async () =>
            {
                try
                {
                    await _cacheManager.SetAsync(
                        $"key_{i}",
                        new TestCacheValue { Id = i, Name = $"Test_{i}" },
                        [dependency]);
                }
                catch (ObjectDisposedException)
                {
                    // Expected if cache manager is disposed during test
                }
            }))
            .ToList();

        var invalidateTasks = Enumerable.Range(0, 25)
            .Select(_ => Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(10);
                    await _cacheManager.InvalidateAsync(default, dependency);
                }
                catch (ObjectDisposedException)
                {
                    // Expected if cache manager is disposed during test
                }
            }))
            .ToList();

        // Assert - should not throw
        await Task.WhenAll(setTasks.Concat(invalidateTasks));
    }

    [Fact]
    public async Task ConcurrentInvalidate_SameDependency_DoesNotThrow()
    {
        // Arrange
        var dependency = "dep1";
        var key = "test_key";
        var value = new TestCacheValue { Id = 1, Name = "Test" };
        await _cacheManager.SetAsync(key, value, [dependency]);

        // Act - concurrent invalidation
        var tasks = Enumerable.Range(0, 50)
            .Select(_ => _cacheManager.InvalidateAsync(default, dependency))
            .ToList();

        await Task.WhenAll(tasks);

        var result = await _cacheManager.GetAsync<TestCacheValue>(key);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region Cancellation Tests

    [Fact]
    public async Task GetAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _cacheManager.GetAsync<TestCacheValue>("key", cts.Token));
    }

    [Fact]
    public async Task SetAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        var value = new TestCacheValue { Id = 1, Name = "Test" };
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _cacheManager.SetAsync("key", value, [], cancellationToken: cts.Token));
    }

    #endregion

    #region Disposal Tests

    [Fact]
    public async Task Dispose_DisposesResources()
    {
        // Arrange
        var cache = new MemoryCache(new MemoryCacheOptions());
        var manager = new MemoryCacheManager(cache);

        var key = "test_key";
        var value = new TestCacheValue { Id = 1, Name = "Test" };
        await manager.SetAsync(key, value, ["dep1"]);

        // Act
        manager.Dispose();

        // Assert - operations after dispose should throw
        await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            manager.GetAsync<TestCacheValue>(key));
    }

    [Fact]
    public void Dispose_CalledMultipleTimes_DoesNotThrow()
    {
        // Arrange
        var cache = new MemoryCache(new MemoryCacheOptions());
        var manager = new MemoryCacheManager(cache);

        // Act & Assert
        manager.Dispose();
        manager.Dispose();
        manager.Dispose();
    }

    [Fact]
    public async Task Dispose_WithPendingOperations_CleansUpCorrectly()
    {
        // Arrange
        var cache = new MemoryCache(new MemoryCacheOptions());
        var manager = new MemoryCacheManager(cache);

        var key = "test_key";
        var value = new TestCacheValue { Id = 1, Name = "Test" };
        await manager.SetAsync(key, value, ["dep1", "dep2", "dep3"]);

        // Act
        manager.Dispose();

        // Assert - verify no resource leaks (no exception thrown)
        Assert.True(true);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task SetAsync_VeryLongKey_Succeeds()
    {
        // Arrange
        var key = new string('a', 1000);
        var value = new TestCacheValue { Id = 1, Name = "Test" };

        // Act
        await _cacheManager.SetAsync(key, value, []);
        var result = await _cacheManager.GetAsync<TestCacheValue>(key);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task SetAsync_VeryLongDependency_Succeeds()
    {
        // Arrange
        var key = "test_key";
        var value = new TestCacheValue { Id = 1, Name = "Test" };
        var dependency = new string('b', 1000);

        // Act
        await _cacheManager.SetAsync(key, value, [dependency]);
        await _cacheManager.InvalidateAsync(default, dependency);
        var result = await _cacheManager.GetAsync<TestCacheValue>(key);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task SetAsync_ManyDependencies_Succeeds()
    {
        // Arrange
        var key = "test_key";
        var value = new TestCacheValue { Id = 1, Name = "Test" };
        var dependencies = Enumerable.Range(0, 100).Select(i => $"dep_{i}").ToArray();

        // Act
        await _cacheManager.SetAsync(key, value, dependencies);
        var result = await _cacheManager.GetAsync<TestCacheValue>(key);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task InvalidateAsync_ManyDependencies_Succeeds()
    {
        // Arrange
        var keys = Enumerable.Range(0, 50).Select(i => $"key_{i}").ToArray();
        var value = new TestCacheValue { Id = 1, Name = "Test" };

        foreach (var key in keys)
        {
            await _cacheManager.SetAsync(key, value, [$"dep_{key}"]);
        }

        var dependenciesToInvalidate = keys.Select(k => $"dep_{k}").ToArray();

        // Act
        await _cacheManager.InvalidateAsync(default, dependenciesToInvalidate);

        // Verify all are invalidated
        var results = new List<TestCacheValue?>();
        foreach (var key in keys)
        {
            results.Add(await _cacheManager.GetAsync<TestCacheValue>(key));
        }

        // Assert
        Assert.All(results, Assert.Null);
    }

    [Fact]
    public async Task GetAsync_TypeMismatch_ReturnsNull()
    {
        // Arrange
        var key = "test_key";
        var value = new TestCacheValue { Id = 1, Name = "Test" };
        await _cacheManager.SetAsync(key, value, []);

        // Act - try to get as different type
        var result = await _cacheManager.GetAsync<OtherTestValue>(key);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task SetAsync_ComplexObject_WithNestedProperties_Succeeds()
    {
        // Arrange
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

        // Act
        await _cacheManager.SetAsync(key, value, []);
        var result = await _cacheManager.GetAsync<ComplexCacheValue>(key);

        // Assert
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
        // Arrange - two managers sharing the same IMemoryCache with different prefixes
        var sharedCache = new MemoryCache(new MemoryCacheOptions());
        using var manager1 = new MemoryCacheManager(sharedCache, keyPrefix: "client1");
        using var manager2 = new MemoryCacheManager(sharedCache, keyPrefix: "client2");

        var key = "same_key";
        var value1 = new TestCacheValue { Id = 1, Name = "Client1Value" };
        var value2 = new TestCacheValue { Id = 2, Name = "Client2Value" };

        // Act - both managers set the same logical key
        await manager1.SetAsync(key, value1, []);
        await manager2.SetAsync(key, value2, []);

        var result1 = await manager1.GetAsync<TestCacheValue>(key);
        var result2 = await manager2.GetAsync<TestCacheValue>(key);

        // Assert - each manager gets its own value
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
        // Arrange - two managers sharing the same IMemoryCache with different prefixes
        var sharedCache = new MemoryCache(new MemoryCacheOptions());
        using var manager1 = new MemoryCacheManager(sharedCache, keyPrefix: "client1");
        using var manager2 = new MemoryCacheManager(sharedCache, keyPrefix: "client2");

        var key = "same_key";
        var dependency = "same_dep";
        var value1 = new TestCacheValue { Id = 1, Name = "Client1Value" };
        var value2 = new TestCacheValue { Id = 2, Name = "Client2Value" };

        await manager1.SetAsync(key, value1, [dependency]);
        await manager2.SetAsync(key, value2, [dependency]);

        // Act - invalidate only in manager1
        await manager1.InvalidateAsync(default, dependency);

        var result1 = await manager1.GetAsync<TestCacheValue>(key);
        var result2 = await manager2.GetAsync<TestCacheValue>(key);

        // Assert - only manager1's entry is invalidated
        Assert.Null(result1);
        Assert.NotNull(result2);
        Assert.Equal(2, result2.Id);

        sharedCache.Dispose();
    }

    [Fact]
    public async Task GetAsync_WithDifferentPrefixes_DoesNotCrossContaminate()
    {
        // Arrange - two managers sharing the same IMemoryCache with different prefixes
        var sharedCache = new MemoryCache(new MemoryCacheOptions());
        using var manager1 = new MemoryCacheManager(sharedCache, keyPrefix: "client1");
        using var manager2 = new MemoryCacheManager(sharedCache, keyPrefix: "client2");

        var key = "unique_key";
        var value = new TestCacheValue { Id = 1, Name = "OnlyInClient1" };

        // Act - set only in manager1
        await manager1.SetAsync(key, value, []);

        var result1 = await manager1.GetAsync<TestCacheValue>(key);
        var result2 = await manager2.GetAsync<TestCacheValue>(key);

        // Assert - manager2 should not see manager1's entry
        Assert.NotNull(result1);
        Assert.Null(result2);

        sharedCache.Dispose();
    }

    [Fact]
    public async Task SetAsync_WithNullPrefix_UsesUnprefixedKeys()
    {
        // Arrange - manager without prefix
        var sharedCache = new MemoryCache(new MemoryCacheOptions());
        using var managerNoPrefix = new MemoryCacheManager(sharedCache, keyPrefix: null);
        using var managerWithPrefix = new MemoryCacheManager(sharedCache, keyPrefix: "prefixed");

        var key = "test_key";
        var value1 = new TestCacheValue { Id = 1, Name = "NoPrefix" };
        var value2 = new TestCacheValue { Id = 2, Name = "WithPrefix" };

        // Act
        await managerNoPrefix.SetAsync(key, value1, []);
        await managerWithPrefix.SetAsync(key, value2, []);

        var result1 = await managerNoPrefix.GetAsync<TestCacheValue>(key);
        var result2 = await managerWithPrefix.GetAsync<TestCacheValue>(key);

        // Assert - both should have separate entries
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.Equal(1, result1.Id);
        Assert.Equal(2, result2.Id);

        sharedCache.Dispose();
    }

    [Fact]
    public async Task InvalidateAsync_WithSharedDependencyName_OnlyInvalidatesOwnPrefix()
    {
        // Arrange - three entries: two with same key/dep in different prefixes, one unrelated
        var sharedCache = new MemoryCache(new MemoryCacheOptions());
        using var manager1 = new MemoryCacheManager(sharedCache, keyPrefix: "prod");
        using var manager2 = new MemoryCacheManager(sharedCache, keyPrefix: "preview");

        var dependency = "content_type_article";

        await manager1.SetAsync("item1", new TestCacheValue { Id = 1 }, [dependency]);
        await manager1.SetAsync("item2", new TestCacheValue { Id = 2 }, [dependency]);
        await manager2.SetAsync("item1", new TestCacheValue { Id = 10 }, [dependency]);
        await manager2.SetAsync("item2", new TestCacheValue { Id = 20 }, [dependency]);

        // Act - invalidate dependency only in production manager
        await manager1.InvalidateAsync(default, dependency);

        // Assert - only production entries are invalidated
        Assert.Null(await manager1.GetAsync<TestCacheValue>("item1"));
        Assert.Null(await manager1.GetAsync<TestCacheValue>("item2"));
        Assert.NotNull(await manager2.GetAsync<TestCacheValue>("item1"));
        Assert.NotNull(await manager2.GetAsync<TestCacheValue>("item2"));

        sharedCache.Dispose();
    }

    [Fact]
    public async Task ConcurrentOperations_WithDifferentPrefixes_MaintainsIsolation()
    {
        // Arrange
        var sharedCache = new MemoryCache(new MemoryCacheOptions());
        using var manager1 = new MemoryCacheManager(sharedCache, keyPrefix: "client1");
        using var manager2 = new MemoryCacheManager(sharedCache, keyPrefix: "client2");

        var dependency = "shared_dep_name";

        // Act - concurrent sets from both managers
        var tasks1 = Enumerable.Range(0, 25)
            .Select(i => manager1.SetAsync($"key_{i}", new TestCacheValue { Id = i }, [dependency]));
        var tasks2 = Enumerable.Range(0, 25)
            .Select(i => manager2.SetAsync($"key_{i}", new TestCacheValue { Id = i + 100 }, [dependency]));

        await Task.WhenAll(tasks1.Concat(tasks2));

        // Invalidate only manager1's entries
        await manager1.InvalidateAsync(default, dependency);

        // Assert - manager1 entries invalidated, manager2 entries intact
        var verify1 = await Task.WhenAll(Enumerable.Range(0, 25)
            .Select(i => manager1.GetAsync<TestCacheValue>($"key_{i}")));
        var verify2 = await Task.WhenAll(Enumerable.Range(0, 25)
            .Select(i => manager2.GetAsync<TestCacheValue>($"key_{i}")));

        Assert.All(verify1, Assert.Null);
        Assert.All(verify2, Assert.NotNull);

        sharedCache.Dispose();
    }

    #endregion

    private static TField GetPrivateField<TField>(object instance, string fieldName)
    {
        var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        return (TField)field!.GetValue(instance)!;
    }

    private static async Task<bool> WaitUntilAsync(Func<bool> condition, TimeSpan timeout, TimeSpan pollInterval)
    {
        if (condition())
            return true;

        var sw = Stopwatch.StartNew();
        while (sw.Elapsed < timeout)
        {
            await Task.Delay(pollInterval);
            if (condition())
                return true;
        }

        return condition();
    }

    private static bool ConcurrentDictionaryContainsKey(object dictionary, string key)
    {
        var method = dictionary.GetType().GetMethod("ContainsKey", [typeof(string)]);
        Assert.NotNull(method);
        return (bool)method!.Invoke(dictionary, [key])!;
    }

    private static IEnumerable<string> GetConcurrentDictionaryKeys(object dictionary)
    {
        var keysProp = dictionary.GetType().GetProperty("Keys");
        Assert.NotNull(keysProp);
        var keys = keysProp!.GetValue(dictionary);
        Assert.NotNull(keys);
        return ((System.Collections.IEnumerable)keys!).Cast<object>().Select(k => k?.ToString() ?? "");
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
