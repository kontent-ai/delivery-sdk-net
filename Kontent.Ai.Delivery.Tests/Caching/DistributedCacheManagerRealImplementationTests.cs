using System;
using System.Linq;
using System.Threading.Tasks;
using Kontent.Ai.Delivery.Caching;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Kontent.Ai.Delivery.Tests.Caching;

/// <summary>
/// Integration tests for DistributedCacheManager using Microsoft's official AddDistributedMemoryCache implementation.
/// These tests verify that our cache manager works correctly with a real IDistributedCache implementation,
/// not just our custom mock.
/// </summary>
public class DistributedCacheManagerRealImplementationTests
{
    private readonly IDistributedCache _distributedCache;
    private readonly DistributedCacheManager _cacheManager;

    public DistributedCacheManagerRealImplementationTests()
    {
        // Use Microsoft's official in-memory distributed cache implementation
        var services = new ServiceCollection();
        services.AddDistributedMemoryCache();
        var serviceProvider = services.BuildServiceProvider();

        _distributedCache = serviceProvider.GetRequiredService<IDistributedCache>();
        _cacheManager = new DistributedCacheManager(_distributedCache, defaultExpiration: TimeSpan.FromMinutes(5));
    }

    #region Basic Operations

    [Fact]
    public async Task SetAsync_ThenGetAsync_WithRealImplementation_ReturnsValue()
    {
        // Arrange
        var key = "real_test_key";
        var value = new TestValue { Id = 42, Name = "Integration Test" };
        var dependencies = new[] { "dep1" };

        // Act
        await _cacheManager.SetAsync(key, value, dependencies);
        var result = await _cacheManager.GetAsync<TestValue>(key);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(value.Id, result.Id);
        Assert.Equal(value.Name, result.Name);
    }

    [Fact]
    public async Task GetAsync_NonExistentKey_WithRealImplementation_ReturnsNull()
    {
        // Act
        var result = await _cacheManager.GetAsync<TestValue>("non_existent_key_real");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task SetAsync_OverwritesExistingKey_WithRealImplementation()
    {
        // Arrange
        var key = "overwrite_key_real";
        var value1 = new TestValue { Id = 1, Name = "First" };
        var value2 = new TestValue { Id = 2, Name = "Second" };

        // Act
        await _cacheManager.SetAsync(key, value1, []);
        await _cacheManager.SetAsync(key, value2, []);
        var result = await _cacheManager.GetAsync<TestValue>(key);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(value2.Id, result.Id);
        Assert.Equal(value2.Name, result.Name);
    }

    #endregion

    #region Dependency Tracking and Invalidation

    [Fact]
    public async Task InvalidateAsync_WithRealImplementation_RemovesCacheEntry()
    {
        // Arrange
        var key = "invalidate_test_key";
        var value = new TestValue { Id = 100, Name = "To Be Invalidated" };
        var dependency = "invalidate_dep";

        await _cacheManager.SetAsync(key, value, [dependency]);

        // Act
        await _cacheManager.InvalidateAsync(default, dependency);
        var result = await _cacheManager.GetAsync<TestValue>(key);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task InvalidateAsync_SharedDependency_WithRealImplementation_RemovesAllEntries()
    {
        // Arrange
        var key1 = "shared_key1";
        var key2 = "shared_key2";
        var key3 = "other_key";
        var value = new TestValue { Id = 1, Name = "Test" };
        var sharedDependency = "shared_real_dep";

        await _cacheManager.SetAsync(key1, value, [sharedDependency]);
        await _cacheManager.SetAsync(key2, value, [sharedDependency]);
        await _cacheManager.SetAsync(key3, value, ["other_dep_real"]);

        // Act
        await _cacheManager.InvalidateAsync(default, sharedDependency);

        var result1 = await _cacheManager.GetAsync<TestValue>(key1);
        var result2 = await _cacheManager.GetAsync<TestValue>(key2);
        var result3 = await _cacheManager.GetAsync<TestValue>(key3);

        // Assert
        Assert.Null(result1);
        Assert.Null(result2);
        Assert.NotNull(result3); // key3 should still exist
    }

    [Fact]
    public async Task InvalidateAsync_MultipleDependencies_WithRealImplementation_RemovesCorrectEntries()
    {
        // Arrange
        var key1 = "multi_key1";
        var key2 = "multi_key2";
        var key3 = "multi_key3";
        var value = new TestValue { Id = 1, Name = "Test" };

        await _cacheManager.SetAsync(key1, value, ["multi_dep1"]);
        await _cacheManager.SetAsync(key2, value, ["multi_dep2"]);
        await _cacheManager.SetAsync(key3, value, ["multi_dep3"]);

        // Act
        await _cacheManager.InvalidateAsync(default, "multi_dep1", "multi_dep2");

        var result1 = await _cacheManager.GetAsync<TestValue>(key1);
        var result2 = await _cacheManager.GetAsync<TestValue>(key2);
        var result3 = await _cacheManager.GetAsync<TestValue>(key3);

        // Assert
        Assert.Null(result1);
        Assert.Null(result2);
        Assert.NotNull(result3); // key3 should still exist
    }

    #endregion

    #region Complex Object Serialization

    [Fact]
    public async Task SetAsync_ComplexObject_WithRealImplementation_SerializesCorrectly()
    {
        // Arrange
        var key = "complex_real_key";
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

        // Act
        await _cacheManager.SetAsync(key, value, []);
        var result = await _cacheManager.GetAsync<ComplexValue>(key);

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
    public async Task SetAsync_ObjectWithNullProperties_WithRealImplementation_HandlesCorrectly()
    {
        // Arrange
        var key = "null_props_real_key";
        var value = new TestValue { Id = 99, Name = null };

        // Act
        await _cacheManager.SetAsync(key, value, []);
        var result = await _cacheManager.GetAsync<TestValue>(key);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(value.Id, result.Id);
        Assert.Null(result.Name);
    }

    [Fact]
    public async Task SetAsync_ObjectWithCircularReference_WithRealImplementation_HandlesWithReferenceHandler()
    {
        // Arrange
        var key = "circular_real_key";
        var value = new CircularValue { Id = 1, Name = "Parent" };
        value.Self = value; // Circular reference

        // Act
        await _cacheManager.SetAsync(key, value, []);
        var result = await _cacheManager.GetAsync<CircularValue>(key);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(value.Id, result.Id);
        Assert.Equal(value.Name, result.Name);
        Assert.NotNull(result.Self);
        Assert.Same(result, result.Self); // Verify circular reference is preserved
    }

    #endregion

    #region Concurrent Operations

    [Fact]
    public async Task ConcurrentSet_WithRealImplementation_HandlesGracefully()
    {
        // Arrange
        var tasks = Enumerable.Range(0, 20)
            .Select(i => _cacheManager.SetAsync(
                $"concurrent_real_key_{i}",
                new TestValue { Id = i, Name = $"Test_{i}" },
                [$"concurrent_real_dep_{i}"]))
            .ToArray();

        // Act
        await Task.WhenAll(tasks);

        // Assert - verify all entries were stored
        var verifyTasks = Enumerable.Range(0, 20)
            .Select(i => _cacheManager.GetAsync<TestValue>($"concurrent_real_key_{i}"))
            .ToArray();

        var results = await Task.WhenAll(verifyTasks);
        Assert.All(results, Assert.NotNull);
    }

    [Fact]
    public async Task ConcurrentGet_WithRealImplementation_ReturnsConsistentResults()
    {
        // Arrange
        var key = "concurrent_get_real_key";
        var value = new TestValue { Id = 42, Name = "Concurrent Test" };
        await _cacheManager.SetAsync(key, value, []);

        // Act - concurrent reads
        var tasks = Enumerable.Range(0, 50)
            .Select(_ => _cacheManager.GetAsync<TestValue>(key))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.All(results, r =>
        {
            Assert.NotNull(r);
            Assert.Equal(value.Id, r.Id);
            Assert.Equal(value.Name, r.Name);
        });
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task SetAsync_VeryLongKey_WithRealImplementation_Succeeds()
    {
        // Arrange
        var key = new string('x', 500);
        var value = new TestValue { Id = 1, Name = "Long Key Test" };

        // Act
        await _cacheManager.SetAsync(key, value, []);
        var result = await _cacheManager.GetAsync<TestValue>(key);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(value.Id, result.Id);
    }

    [Fact]
    public async Task SetAsync_ManyDependencies_WithRealImplementation_TracksAllDependencies()
    {
        // Arrange
        var key = "many_deps_real_key";
        var value = new TestValue { Id = 1, Name = "Test" };
        var dependencies = Enumerable.Range(0, 50).Select(i => $"real_dep_{i}").ToArray();

        // Act
        await _cacheManager.SetAsync(key, value, dependencies);

        // Invalidate one dependency
        await _cacheManager.InvalidateAsync(default, "real_dep_25");
        var result = await _cacheManager.GetAsync<TestValue>(key);

        // Assert - entry should be invalidated
        Assert.Null(result);
    }

    [Fact]
    public async Task InvalidateAsync_NonExistentDependency_WithRealImplementation_DoesNotThrow()
    {
        // Act & Assert
        await _cacheManager.InvalidateAsync(default, "non_existent_real_dep");
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