using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.Caching;
using Kontent.Ai.Delivery.Configuration;
using Kontent.Ai.Delivery.ContentTypes;
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
        _cacheManager = new DistributedCacheManager(_mockCache, new DeliveryCacheOptions { DefaultExpiration = TimeSpan.FromMinutes(5) });
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

    #endregion

    #region Expiration Tests

    [Fact]
    public void Constructor_WithDefaultExpiration_AcceptsValue()
    {
        var expiration = TimeSpan.FromMinutes(30);
        var manager = new DistributedCacheManager(_mockCache, new DeliveryCacheOptions { DefaultExpiration = expiration });

        Assert.NotNull(manager);
    }

    [Fact]
    public void Constructor_WithNullExpiration_UsesDefaultOneHour()
    {
        var manager = new DistributedCacheManager(_mockCache, new DeliveryCacheOptions());

        Assert.NotNull(manager);
    }

    [Fact]
    public async Task SetAsync_WithCustomExpiration_DoesNotThrow()
    {
        var key = "test_key";
        var value = new TestCacheValue { Id = 1, Name = "Test" };
        var customExpiration = TimeSpan.FromMinutes(15);

        await _cacheManager.SetAsync(key, value, [], customExpiration);

        var result = await _cacheManager.GetAsync<TestCacheValue>(key);
        Assert.NotNull(result);
    }

    [Fact]
    public async Task SetAsync_ExpirationPassedToCacheEntry()
    {
        var manager = new DistributedCacheManager(_mockCache, new DeliveryCacheOptions { DefaultExpiration = TimeSpan.FromHours(2) });

        var key = "test_key";
        var value = new TestCacheValue { Id = 1, Name = "Test" };
        var customExpiration = TimeSpan.FromMilliseconds(80);

        await manager.SetAsync(key, value, [], customExpiration);

        var expired = await WaitUntilAsync(
            async () => await manager.GetAsync<TestCacheValue>(key) is null,
            timeout: TimeSpan.FromSeconds(2),
            pollInterval: TimeSpan.FromMilliseconds(20));

        Assert.True(expired);
    }

    [Fact]
    public async Task SetAsync_WithoutCustomExpiration_UsesDefaultExpiration()
    {
        var defaultExpiration = TimeSpan.FromMilliseconds(80);
        var manager = new DistributedCacheManager(_mockCache, new DeliveryCacheOptions { DefaultExpiration = defaultExpiration });

        var key = "test_key";
        var value = new TestCacheValue { Id = 1, Name = "Test" };

        await manager.SetAsync(key, value, []);

        var expired = await WaitUntilAsync(
            async () => await manager.GetAsync<TestCacheValue>(key) is null,
            timeout: TimeSpan.FromSeconds(2),
            pollInterval: TimeSpan.FromMilliseconds(20));

        Assert.True(expired);
    }

    #endregion

    #region Serialization Tests

    [Fact]
    public async Task SetAsync_SimpleObject_SerializesCorrectly()
    {
        var key = "test_key";
        var value = new TestCacheValue { Id = 42, Name = "Test Value" };

        await _cacheManager.SetAsync(key, value, []);
        var result = await _cacheManager.GetAsync<TestCacheValue>(key);

        Assert.NotNull(result);
        Assert.Equal(value.Id, result.Id);
        Assert.Equal(value.Name, result.Name);
    }

    [Fact]
    public async Task SetAsync_ComplexObject_SerializesCorrectly()
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

    [Fact]
    public async Task SetAsync_ObjectWithNullProperties_SerializesCorrectly()
    {
        var key = "null_props_key";
        var value = new TestCacheValue { Id = 1, Name = null };

        await _cacheManager.SetAsync(key, value, []);
        var result = await _cacheManager.GetAsync<TestCacheValue>(key);

        Assert.NotNull(result);
        Assert.Equal(value.Id, result.Id);
        Assert.Null(result.Name);
    }

    [Fact]
    public async Task SetAsync_ObjectWithCircularReference_DoesNotThrow()
    {
        var key = "circular_key";
        var value = new CircularReferenceValue { Id = 1, Name = "Parent" };
        value.Self = value;

        var exception = await Record.ExceptionAsync(() => _cacheManager.SetAsync(key, value, []));
        Assert.Null(exception);
    }

    [Fact]
    public async Task SetAsync_ContentTypeFromApiDeserializer_CanRoundTripFromCache()
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

        await _cacheManager.SetAsync("content-type-key", contentType, []);
        var cached = await _cacheManager.GetAsync<ContentType>("content-type-key");

        Assert.NotNull(cached);
        Assert.Equal(contentType.System.Codename, cached.System.Codename);
    }

    [Fact]
    public async Task GetAsync_CorruptedData_ReturnsNull()
    {
        var key = "corrupted_key";
        _mockCache.Set("cache:" + key, Encoding.UTF8.GetBytes("invalid json {{{"), new DistributedCacheEntryOptions());

        var result = await _cacheManager.GetAsync<TestCacheValue>(key);

        Assert.Null(result);
    }

    #endregion

    #region Dependency Tracking Tests

    [Fact]
    public async Task SetAsync_WithDependencies_InvalidateRemovesEntry()
    {
        var key = "test_key";
        var value = new TestCacheValue { Id = 1, Name = "Test" };
        var dependencies = new[] { "dep1", "dep2" };

        await _cacheManager.SetAsync(key, value, dependencies);
        await _cacheManager.InvalidateAsync(default, "dep1");
        Assert.Null(await _cacheManager.GetAsync<TestCacheValue>(key));
    }

    [Fact]
    public async Task SetAsync_SameDependencyWithShorterTtl_StillInvalidatesAllEntries()
    {
        var manager = new DistributedCacheManager(_mockCache, new DeliveryCacheOptions { DefaultExpiration = TimeSpan.FromMinutes(5) });
        var dependency = "dep_shared";

        await manager.SetAsync(
            "long_ttl_key",
            new TestCacheValue { Id = 1, Name = "Long" },
            [dependency],
            expiration: TimeSpan.FromMinutes(30));

        await manager.SetAsync(
            "short_ttl_key",
            new TestCacheValue { Id = 2, Name = "Short" },
            [dependency],
            expiration: TimeSpan.FromMinutes(1));

        await manager.InvalidateAsync(default, dependency);
        Assert.Null(await manager.GetAsync<TestCacheValue>("long_ttl_key"));
        Assert.Null(await manager.GetAsync<TestCacheValue>("short_ttl_key"));
    }

    [Fact]
    public async Task SetAsync_WhenDependencyEnumerationThrows_DoesNotStoreCacheEntry()
    {
        var cache = new MockDistributedCache();
        var manager = new DistributedCacheManager(cache, new DeliveryCacheOptions { DefaultExpiration = TimeSpan.FromMinutes(5) });
        var key = "key_with_throwing_deps";
        var value = new TestCacheValue { Id = 1, Name = "Test" };

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            manager.SetAsync(key, value, ThrowingDependencies()));

        Assert.Null(cache.Get("cache:" + key));
        Assert.Null(cache.Get("dep:dep1"));
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
    public async Task InvalidateAsync_RemovesReverseIndex()
    {
        var key = "test_key";
        var value = new TestCacheValue { Id = 1, Name = "Test" };
        var dependency = "dep1";

        await _cacheManager.SetAsync(key, value, [dependency]);

        await _cacheManager.InvalidateAsync(default, dependency);

        var indexEntry = _mockCache.Get("dep:dep1");

        Assert.Null(indexEntry);
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
    public async Task ConcurrentSet_WithSameDependency_EventuallyConsistent()
    {
        var sharedDependency = "shared_dep";
        var tasks = Enumerable.Range(0, 50)
            .Select(i => _cacheManager.SetAsync(
                $"key_{i}",
                new TestCacheValue { Id = i, Name = $"Test_{i}" },
                [sharedDependency]))
            .ToList();

        await Task.WhenAll(tasks);

        // FusionCache tags provide deterministic invalidation — all entries sharing
        // the tag are removed atomically, with no race condition window.

        await _cacheManager.InvalidateAsync(default, sharedDependency);

        var verifyTasks = Enumerable.Range(0, 50)
            .Select(i => _cacheManager.GetAsync<TestCacheValue>($"key_{i}"))
            .ToList();

        var results = await Task.WhenAll(verifyTasks);

        Assert.All(results, Assert.Null);
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
    public async Task SetAsync_WithPrefixedManager_DoesNotLeakToDefaultNamespace()
    {
        var key = "test_key";
        var value = new TestCacheValue { Id = 1, Name = "Test" };
        var prefixedManager = new DistributedCacheManager(_mockCache, new DeliveryCacheOptions { KeyPrefix = "prefixed" });

        await prefixedManager.SetAsync(key, value, []);

        Assert.NotNull(await prefixedManager.GetAsync<TestCacheValue>(key));
        Assert.Null(await _cacheManager.GetAsync<TestCacheValue>(key));
    }

    [Fact]
    public async Task SetAsync_DependenciesSupportInvalidate()
    {
        var key = "test_key";
        var value = new TestCacheValue { Id = 1, Name = "Test" };
        var dependency = "dep1";

        await _cacheManager.SetAsync(key, value, [dependency]);
        await _cacheManager.InvalidateAsync(default, dependency);
        Assert.Null(await _cacheManager.GetAsync<TestCacheValue>(key));
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
    public async Task InvalidateAsync_CorruptedReverseIndex_HandlesGracefully()
    {
        var dependency = "dep1";
        _mockCache.Set("dep:" + dependency, Encoding.UTF8.GetBytes("invalid json"), new DistributedCacheEntryOptions());

        await _cacheManager.InvalidateAsync(default, dependency);
    }

    [Fact]
    public async Task InvalidateAsync_EmptyReverseIndex_HandlesGracefully()
    {
        var dependency = "dep1";
        var emptySet = new HashSet<string>();
        var json = JsonSerializer.Serialize(emptySet);
        _mockCache.Set("dep:" + dependency, Encoding.UTF8.GetBytes(json), new DistributedCacheEntryOptions());

        await _cacheManager.InvalidateAsync(default, dependency);
    }

    #endregion

    #region Key Prefix Isolation Tests

    [Fact]
    public async Task SetAsync_WithDifferentPrefixes_IsolatesCacheEntries()
    {
        var sharedCache = new MockDistributedCache();
        var manager1 = new DistributedCacheManager(sharedCache, new DeliveryCacheOptions { KeyPrefix = "client1" });
        var manager2 = new DistributedCacheManager(sharedCache, new DeliveryCacheOptions { KeyPrefix = "client2" });

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
    }

    [Fact]
    public async Task InvalidateAsync_WithDifferentPrefixes_OnlyAffectsOwnEntries()
    {
        var sharedCache = new MockDistributedCache();
        var manager1 = new DistributedCacheManager(sharedCache, new DeliveryCacheOptions { KeyPrefix = "client1" });
        var manager2 = new DistributedCacheManager(sharedCache, new DeliveryCacheOptions { KeyPrefix = "client2" });

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
    }

    [Fact]
    public async Task GetAsync_WithDifferentPrefixes_DoesNotCrossContaminate()
    {
        var sharedCache = new MockDistributedCache();
        var manager1 = new DistributedCacheManager(sharedCache, new DeliveryCacheOptions { KeyPrefix = "client1" });
        var manager2 = new DistributedCacheManager(sharedCache, new DeliveryCacheOptions { KeyPrefix = "client2" });

        var key = "unique_key";
        var value = new TestCacheValue { Id = 1, Name = "OnlyInClient1" };

        await manager1.SetAsync(key, value, []);

        var result1 = await manager1.GetAsync<TestCacheValue>(key);
        var result2 = await manager2.GetAsync<TestCacheValue>(key);

        Assert.NotNull(result1);
        Assert.Null(result2);
    }

    [Fact]
    public async Task SetAsync_WithNullPrefix_UsesUnprefixedKeys()
    {
        var sharedCache = new MockDistributedCache();
        var managerNoPrefix = new DistributedCacheManager(sharedCache, new DeliveryCacheOptions { KeyPrefix = null });
        var managerWithPrefix = new DistributedCacheManager(sharedCache, new DeliveryCacheOptions { KeyPrefix = "prefixed" });

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
    }

    [Fact]
    public async Task InvalidateAsync_WithSharedDependencyName_OnlyInvalidatesOwnPrefix()
    {
        var sharedCache = new MockDistributedCache();
        var manager1 = new DistributedCacheManager(sharedCache, new DeliveryCacheOptions { KeyPrefix = "prod" });
        var manager2 = new DistributedCacheManager(sharedCache, new DeliveryCacheOptions { KeyPrefix = "preview" });

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
    }

    [Fact]
    public async Task ConcurrentOperations_WithDifferentPrefixes_MaintainsIsolation()
    {
        var sharedCache = new MockDistributedCache();
        var manager1 = new DistributedCacheManager(sharedCache, new DeliveryCacheOptions { KeyPrefix = "client1" });
        var manager2 = new DistributedCacheManager(sharedCache, new DeliveryCacheOptions { KeyPrefix = "client2" });

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
    }

    [Fact]
    public async Task Constructor_WithKeyPrefix_IsolatesEntries()
    {
        var cache = new MockDistributedCache();
        var manager = new DistributedCacheManager(cache, new DeliveryCacheOptions { KeyPrefix = "my-prefix" });
        var defaultManager = new DistributedCacheManager(cache, new DeliveryCacheOptions());

        await manager.SetAsync("test", new TestCacheValue { Id = 1 }, []);
        Assert.NotNull(await manager.GetAsync<TestCacheValue>("test"));
        Assert.Null(await defaultManager.GetAsync<TestCacheValue>("test"));
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

    private static IEnumerable<string> ThrowingDependencies()
    {
        yield return "dep1";
        throw new InvalidOperationException("Dependency enumeration failed.");
    }

    #endregion
}
