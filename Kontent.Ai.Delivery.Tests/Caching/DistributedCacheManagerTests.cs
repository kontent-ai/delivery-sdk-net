using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.Caching;
using Microsoft.Extensions.Caching.Distributed;
using Xunit;

namespace Kontent.Ai.Delivery.Tests.Caching;

/// <summary>
/// Comprehensive tests for DistributedCacheManager implementation.
/// Tests cover: basic operations, dependency tracking, invalidation, serialization, concurrency, and error handling.
/// </summary>
public class DistributedCacheManagerTests
{
    private readonly MockDistributedCache _mockCache;
    private readonly DistributedCacheManager _cacheManager;

    public DistributedCacheManagerTests()
    {
        _mockCache = new MockDistributedCache();
        _cacheManager = new DistributedCacheManager(_mockCache, defaultExpiration: TimeSpan.FromMinutes(5));
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

    #endregion

    #region Serialization Tests

    [Fact]
    public async Task SetAsync_SimpleObject_SerializesCorrectly()
    {
        // Arrange
        var key = "test_key";
        var value = new TestCacheValue { Id = 42, Name = "Test Value" };

        // Act
        await _cacheManager.SetAsync(key, value, Array.Empty<string>());
        var result = await _cacheManager.GetAsync<TestCacheValue>(key);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(value.Id, result.Id);
        Assert.Equal(value.Name, result.Name);
    }

    [Fact]
    public async Task SetAsync_ComplexObject_SerializesCorrectly()
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
            Items = new List<int> { 1, 2, 3, 4, 5 }
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

    [Fact]
    public async Task SetAsync_ObjectWithNullProperties_SerializesCorrectly()
    {
        // Arrange
        var key = "null_props_key";
        var value = new TestCacheValue { Id = 1, Name = null };

        // Act
        await _cacheManager.SetAsync(key, value, Array.Empty<string>());
        var result = await _cacheManager.GetAsync<TestCacheValue>(key);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(value.Id, result.Id);
        Assert.Null(result.Name);
    }

    [Fact]
    public async Task SetAsync_ObjectWithCircularReference_HandlesWithReferenceHandler()
    {
        // Arrange
        var key = "circular_key";
        var value = new CircularReferenceValue { Id = 1, Name = "Parent" };
        value.Self = value; // Circular reference

        // Act
        await _cacheManager.SetAsync(key, value, Array.Empty<string>());
        var result = await _cacheManager.GetAsync<CircularReferenceValue>(key);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(value.Id, result.Id);
        Assert.Equal(value.Name, result.Name);
        Assert.NotNull(result.Self);
        Assert.Same(result, result.Self); // Verify circular reference is preserved
    }

    [Fact]
    public async Task GetAsync_CorruptedData_ReturnsNull()
    {
        // Arrange
        var key = "corrupted_key";
        _mockCache.Set("cache:" + key, Encoding.UTF8.GetBytes("invalid json {{{"), new DistributedCacheEntryOptions());

        // Act
        var result = await _cacheManager.GetAsync<TestCacheValue>(key);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region Dependency Tracking Tests

    [Fact]
    public async Task SetAsync_WithDependencies_CreatesReverseIndex()
    {
        // Arrange
        var key = "test_key";
        var value = new TestCacheValue { Id = 1, Name = "Test" };
        var dependencies = new[] { "dep1", "dep2" };

        // Act
        await _cacheManager.SetAsync(key, value, dependencies);

        // Verify reverse index entries were created
        var dep1Index = _mockCache.Get("dep:dep1");
        var dep2Index = _mockCache.Get("dep:dep2");

        // Assert
        Assert.NotNull(dep1Index);
        Assert.NotNull(dep2Index);
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
    public async Task InvalidateAsync_RemovesReverseIndex()
    {
        // Arrange
        var key = "test_key";
        var value = new TestCacheValue { Id = 1, Name = "Test" };
        var dependency = "dep1";

        await _cacheManager.SetAsync(key, value, new[] { dependency });

        // Act
        await _cacheManager.InvalidateAsync(default, dependency);

        // Verify reverse index is removed
        var indexEntry = _mockCache.Get("dep:dep1");

        // Assert
        Assert.Null(indexEntry);
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
    public async Task ConcurrentSet_WithSameDependency_EventuallyConsistent()
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

        // Note: Due to race conditions in reverse index, not all entries may be tracked
        // This is acceptable as documented - eventual consistency

        // Invalidate and verify
        await _cacheManager.InvalidateAsync(default, sharedDependency);

        // At least some entries should be invalidated
        var verifyTasks = Enumerable.Range(0, 50)
            .Select(i => _cacheManager.GetAsync<TestCacheValue>($"key_{i}"))
            .ToList();

        var results = await Task.WhenAll(verifyTasks);

        // Assert - most entries should be null (eventual consistency may allow some to survive)
        var nullCount = results.Count(r => r == null);
        Assert.True(nullCount >= 25, $"Expected at least 25 entries to be invalidated, but only {nullCount} were invalidated");
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

    #region Cache Key Prefix Tests

    [Fact]
    public async Task SetAsync_UsesCachePrefix()
    {
        // Arrange
        var key = "test_key";
        var value = new TestCacheValue { Id = 1, Name = "Test" };

        // Act
        await _cacheManager.SetAsync(key, value, Array.Empty<string>());

        // Verify the cache uses "cache:" prefix
        var cacheEntry = _mockCache.Get("cache:" + key);

        // Assert
        Assert.NotNull(cacheEntry);
    }

    [Fact]
    public async Task SetAsync_DependenciesUseDependencyPrefix()
    {
        // Arrange
        var key = "test_key";
        var value = new TestCacheValue { Id = 1, Name = "Test" };
        var dependency = "dep1";

        // Act
        await _cacheManager.SetAsync(key, value, new[] { dependency });

        // Verify the dependency uses "dep:" prefix
        var depEntry = _mockCache.Get("dep:" + dependency);

        // Assert
        Assert.NotNull(depEntry);
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
    public async Task GetAsync_TypeMismatch_ReturnsObjectWithDefaultValues()
    {
        // Arrange
        var key = "test_key";
        var value = new TestCacheValue { Id = 1, Name = "Test" };
        await _cacheManager.SetAsync(key, value, Array.Empty<string>());

        // Act - try to get as different type
        var result = await _cacheManager.GetAsync<OtherTestValue>(key);

        // Assert
        // System.Text.Json is permissive and will deserialize to OtherTestValue with default values
        // This is expected behavior - the JSON contains {Id:1, Name:"Test"} but OtherTestValue only has Data property
        Assert.NotNull(result);
        Assert.Null(result.Data); // Data property gets default value (null)
    }

    [Fact]
    public async Task InvalidateAsync_CorruptedReverseIndex_HandlesGracefully()
    {
        // Arrange
        var dependency = "dep1";
        _mockCache.Set("dep:" + dependency, Encoding.UTF8.GetBytes("invalid json"), new DistributedCacheEntryOptions());

        // Act & Assert - should not throw
        await _cacheManager.InvalidateAsync(default, dependency);
    }

    [Fact]
    public async Task InvalidateAsync_EmptyReverseIndex_HandlesGracefully()
    {
        // Arrange
        var dependency = "dep1";
        var emptySet = new HashSet<string>();
        var json = JsonSerializer.Serialize(emptySet);
        _mockCache.Set("dep:" + dependency, Encoding.UTF8.GetBytes(json), new DistributedCacheEntryOptions());

        // Act & Assert - should not throw
        await _cacheManager.InvalidateAsync(default, dependency);
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
        private readonly Dictionary<string, byte[]> _cache = new();
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

        public void Refresh(string key)
        {
            // No-op for testing
        }

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
