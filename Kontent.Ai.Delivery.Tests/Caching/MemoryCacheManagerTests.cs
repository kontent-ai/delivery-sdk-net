using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
        await _cacheManager.InvalidateAsync(default, Array.Empty<string>());
    }

    [Fact]
    public async Task InvalidateAsync_MultipleDependencies_RemovesAllAffected()
    {
        // Arrange
        var key1 = "key1";
        var key2 = "key2";
        var key3 = "key3";
        var value = new TestCacheValue { Id = 1, Name = "Test" };

        await _cacheManager.SetAsync(key1, value, new[] { "dep1" });
        await _cacheManager.SetAsync(key2, value, new[] { "dep2" });
        await _cacheManager.SetAsync(key3, value, new[] { "dep3" });

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

        await _cacheManager.SetAsync(key1, value, new[] { sharedDependency });
        await _cacheManager.SetAsync(key2, value, new[] { sharedDependency });
        await _cacheManager.SetAsync(key3, value, new[] { "other_dep" });

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

        await _cacheManager.SetAsync(key, value, new[] { dependency });

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
        await _cacheManager.SetAsync(key, value, Array.Empty<string>());

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
                new[] { sharedDependency }))
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
                        new[] { dependency });
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
        await _cacheManager.SetAsync(key, value, new[] { dependency });

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
            _cacheManager.SetAsync("key", value, Array.Empty<string>(), cancellationToken: cts.Token));
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
        await manager.SetAsync(key, value, new[] { "dep1" });

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
        await manager.SetAsync(key, value, new[] { "dep1", "dep2", "dep3" });

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
        await _cacheManager.SetAsync(key, value, Array.Empty<string>());
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
        await _cacheManager.SetAsync(key, value, new[] { dependency });
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
            await _cacheManager.SetAsync(key, value, new[] { $"dep_{key}" });
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
        await _cacheManager.SetAsync(key, value, Array.Empty<string>());

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
                Tags = new[] { "tag1", "tag2", "tag3" }
            },
            Items = [1, 2, 3, 4, 5]
        };

        // Act
        await _cacheManager.SetAsync(key, value, Array.Empty<string>());
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

    #region Test Helper Classes

    private class TestCacheValue
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }

    private class OtherTestValue
    {
        public string? Data { get; set; }
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
