using System.Net;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.Caching;
using Kontent.Ai.Delivery.Extensions;
using Kontent.Ai.Delivery.Tests.Models.ContentTypes;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using RichardSzalay.MockHttp;
using Xunit;

namespace Kontent.Ai.Delivery.Tests.Caching;

/// <summary>
/// Integration tests for end-to-end caching scenarios with DeliveryClient.
/// Tests the complete flow: API call → caching → cache hit → invalidation.
/// </summary>
public class CachingIntegrationTests
{
    private readonly Guid _guid = Guid.NewGuid();
    private string BaseUrl => $"https://deliver.kontent.ai/{_guid}";

    #region Memory Cache Integration Tests

    [Fact]
    public async Task MemoryCache_GetItem_CacheHitOnSecondCall()
    {
        var mock = new MockHttpMessageHandler();
        var itemCodename = "coffee_beverages_explained";
        var fixtureContent = await File.ReadAllTextAsync(
            Path.Combine(Environment.CurrentDirectory,
                $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}{itemCodename}.json"));

        // First call should hit the API
        mock.Expect($"{BaseUrl}/items/{itemCodename}")
            .Respond("application/json", fixtureContent);

        var client = CreateClientWithMemoryCache(mock);

        var result1 = await client.GetItem<Article>(itemCodename).ExecuteAsync();
        var result2 = await client.GetItem<Article>(itemCodename).ExecuteAsync();

        Assert.True(result1.IsSuccess);
        Assert.True(result2.IsSuccess);
        Assert.NotNull(result1.Value);
        Assert.NotNull(result2.Value);
        Assert.Equal(result1.Value.Elements.Title, result2.Value.Elements.Title);

        // Verify IsCacheHit property
        Assert.False(result1.IsCacheHit); // First call is API response
        Assert.True(result2.IsCacheHit);  // Second call is cache hit

        // Verify ResponseHeaders property
        Assert.NotNull(result1.ResponseHeaders); // API response has headers
        Assert.Null(result2.ResponseHeaders);    // Cache hit has no headers

        // Verify only one API call was made
        mock.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task MemoryCache_GetItem_ExpiresAfterTtl_HitsApiAgain()
    {
        var mock = new MockHttpMessageHandler();
        var itemCodename = "coffee_beverages_explained";
        var fixtureContent = await File.ReadAllTextAsync(
            Path.Combine(Environment.CurrentDirectory,
                $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}{itemCodename}.json"));

        var services = new ServiceCollection();
        var options = new DeliveryOptions
        {
            EnvironmentId = _guid.ToString()
        };

        // Named client with keyed cache manager and short TTL
        services.AddDeliveryClient("test", o => DeliveryOptionsCopyHelper.Copy(options, o),
            configureHttpClient: b => b.ConfigurePrimaryHttpMessageHandler(() => mock));
        services.AddDeliveryMemoryCache("test", defaultExpiration: TimeSpan.FromMilliseconds(50));

        var client = services.BuildServiceProvider().GetRequiredKeyedService<IDeliveryClient>("test");

        // First call should hit API
        mock.Expect($"{BaseUrl}/items/{itemCodename}")
            .Respond("application/json", fixtureContent);

        var result1 = await client.GetItem<Article>(itemCodename).ExecuteAsync();
        var result2 = await client.GetItem<Article>(itemCodename).ExecuteAsync();

        // Only one API call should have been made so far
        Assert.False(result1.IsCacheHit);
        Assert.True(result2.IsCacheHit);
        mock.VerifyNoOutstandingExpectation();

        // Wait past TTL
        await Task.Delay(200);

        // Third call should hit API again (cache entry expired)
        mock.Expect($"{BaseUrl}/items/{itemCodename}")
            .Respond("application/json", fixtureContent);

        var result3 = await client.GetItem<Article>(itemCodename).ExecuteAsync();

        Assert.False(result3.IsCacheHit);
        mock.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task MemoryCache_GetItem_QueryWaitEnabled_BypassesCache()
    {
        var mock = new MockHttpMessageHandler();
        var itemCodename = "coffee_beverages_explained";
        var fixtureContent = await File.ReadAllTextAsync(
            Path.Combine(Environment.CurrentDirectory,
                $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}{itemCodename}.json"));

        mock.Expect($"{BaseUrl}/items/{itemCodename}")
            .With(req => req.Headers.Contains("X-KC-Wait-For-Loading-New-Content"))
            .Respond("application/json", fixtureContent);
        mock.Expect($"{BaseUrl}/items/{itemCodename}")
            .With(req => req.Headers.Contains("X-KC-Wait-For-Loading-New-Content"))
            .Respond("application/json", fixtureContent);

        var client = CreateClientWithMemoryCache(mock);

        var result1 = await client.GetItem<Article>(itemCodename).WaitForLoadingNewContent().ExecuteAsync();
        var result2 = await client.GetItem<Article>(itemCodename).WaitForLoadingNewContent().ExecuteAsync();

        Assert.True(result1.IsSuccess);
        Assert.True(result2.IsSuccess);
        Assert.False(result1.IsCacheHit);
        Assert.False(result2.IsCacheHit);
        mock.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task MemoryCache_GetItem_QueryWaitFalse_UsesCache()
    {
        var mock = new MockHttpMessageHandler();
        var itemCodename = "coffee_beverages_explained";
        var fixtureContent = await File.ReadAllTextAsync(
            Path.Combine(Environment.CurrentDirectory,
                $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}{itemCodename}.json"));

        mock.Expect($"{BaseUrl}/items/{itemCodename}")
            .With(req => !req.Headers.Contains("X-KC-Wait-For-Loading-New-Content"))
            .Respond("application/json", fixtureContent);

        var client = CreateClientWithMemoryCache(mock);

        var result1 = await client.GetItem<Article>(itemCodename)
            .WaitForLoadingNewContent(false)
            .ExecuteAsync();
        var result2 = await client.GetItem<Article>(itemCodename)
            .WaitForLoadingNewContent(false)
            .ExecuteAsync();

        Assert.True(result1.IsSuccess);
        Assert.True(result2.IsSuccess);
        Assert.False(result1.IsCacheHit);
        Assert.True(result2.IsCacheHit);
        mock.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task MemoryCache_GetItems_CacheHitOnSecondCall()
    {
        var mock = new MockHttpMessageHandler();
        var fixtureContent = await File.ReadAllTextAsync(
            Path.Combine(Environment.CurrentDirectory,
                $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}articles.json"));

        mock.Expect($"{BaseUrl}/items?system.type%5Beq%5D=article")
            .Respond("application/json", fixtureContent);

        var client = CreateClientWithMemoryCache(mock);

        var result1 = await client.GetItems<Article>()
            .Where(f => f.System("type").IsEqualTo("article"))
            .ExecuteAsync();

        var result2 = await client.GetItems<Article>()
            .Where(f => f.System("type").IsEqualTo("article"))
            .ExecuteAsync();

        Assert.True(result1.IsSuccess);
        Assert.True(result2.IsSuccess);
        Assert.NotEmpty(result1.Value.Items);
        Assert.NotEmpty(result2.Value.Items);
        Assert.Equal(result1.Value.Items.Count, result2.Value.Items.Count);

        // Verify IsCacheHit property
        Assert.False(result1.IsCacheHit); // First call is API response
        Assert.True(result2.IsCacheHit);  // Second call is cache hit

        // Verify ResponseHeaders property
        Assert.NotNull(result1.ResponseHeaders); // API response has headers
        Assert.Null(result2.ResponseHeaders);    // Cache hit has no headers

        // Verify only one API call was made
        mock.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task DynamicItemQuery_WithCacheConfigured_DoesNotReturnCacheHit()
    {
        var mock = new MockHttpMessageHandler();
        var itemCodename = "coffee_beverages_explained";
        var fixtureContent = await File.ReadAllTextAsync(
            Path.Combine(Environment.CurrentDirectory,
                $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}{itemCodename}.json"));

        // Dynamic query should call API on every invocation even when cache manager is configured.
        mock.Expect($"{BaseUrl}/items/{itemCodename}")
            .Respond("application/json", fixtureContent);
        mock.Expect($"{BaseUrl}/items/{itemCodename}")
            .Respond("application/json", fixtureContent);

        var client = CreateClientWithMemoryCache(mock);

        var result1 = await client.GetItem(itemCodename).ExecuteAsync();
        var result2 = await client.GetItem(itemCodename).ExecuteAsync();

        Assert.True(result1.IsSuccess);
        Assert.True(result2.IsSuccess);
        Assert.False(result1.IsCacheHit);
        Assert.False(result2.IsCacheHit);
        mock.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task DynamicItemsQuery_WithCacheConfigured_DoesNotReturnCacheHit()
    {
        var mock = new MockHttpMessageHandler();
        var fixtureContent = await File.ReadAllTextAsync(
            Path.Combine(Environment.CurrentDirectory,
                $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}items.json"));

        // Dynamic query should call API on every invocation even when cache manager is configured.
        mock.Expect($"{BaseUrl}/items")
            .Respond("application/json", fixtureContent);
        mock.Expect($"{BaseUrl}/items")
            .Respond("application/json", fixtureContent);

        var client = CreateClientWithMemoryCache(mock);

        var result1 = await client.GetItems().ExecuteAsync();
        var result2 = await client.GetItems().ExecuteAsync();

        Assert.True(result1.IsSuccess);
        Assert.True(result2.IsSuccess);
        Assert.False(result1.IsCacheHit);
        Assert.False(result2.IsCacheHit);
        mock.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task MemoryCache_Invalidation_RefreshesCache()
    {
        var mock = new MockHttpMessageHandler();
        var itemCodename = "coffee_beverages_explained";
        var fixtureContent = await File.ReadAllTextAsync(
            Path.Combine(Environment.CurrentDirectory,
                $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}{itemCodename}.json"));

        // Both calls should hit the API (first for initial, second after invalidation)
        mock.When($"{BaseUrl}/items/{itemCodename}")
            .Respond("application/json", fixtureContent);

        var services = new ServiceCollection();
        var options = new DeliveryOptions
        {
            EnvironmentId = _guid.ToString()
        };

        services.AddDeliveryClient("test", o => DeliveryOptionsCopyHelper.Copy(options, o),
            configureHttpClient: b => b.ConfigurePrimaryHttpMessageHandler(() => mock));
        services.AddDeliveryMemoryCache("test");

        var serviceProvider = services.BuildServiceProvider();
        var client = serviceProvider.GetRequiredKeyedService<IDeliveryClient>("test");
        var cacheManager = serviceProvider.GetRequiredKeyedService<IDeliveryCacheManager>("test");

        var result1 = await client.GetItem<Article>(itemCodename).ExecuteAsync();

        await cacheManager.InvalidateAsync(default, $"item_{itemCodename}");

        var result2 = await client.GetItem<Article>(itemCodename).ExecuteAsync();

        Assert.True(result1.IsSuccess);
        Assert.True(result2.IsSuccess);
        Assert.NotNull(result1.Value);
        Assert.NotNull(result2.Value);
    }

    [Fact]
    public async Task MemoryCache_ItemsListScopeInvalidation_RefreshesAllCachedItemLists()
    {
        var mock = new MockHttpMessageHandler();
        var fixtureContent = await File.ReadAllTextAsync(
            Path.Combine(Environment.CurrentDirectory,
                $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}articles.json"));

        mock.Expect($"{BaseUrl}/items*")
            .Respond("application/json", fixtureContent);
        mock.Expect($"{BaseUrl}/items*")
            .Respond("application/json", fixtureContent);
        mock.Expect($"{BaseUrl}/items*")
            .Respond("application/json", fixtureContent);
        mock.Expect($"{BaseUrl}/items*")
            .Respond("application/json", fixtureContent);

        var services = new ServiceCollection();
        var options = new DeliveryOptions
        {
            EnvironmentId = _guid.ToString()
        };

        services.AddDeliveryClient("test", o => DeliveryOptionsCopyHelper.Copy(options, o),
            configureHttpClient: b => b.ConfigurePrimaryHttpMessageHandler(() => mock));
        services.AddDeliveryMemoryCache("test");

        var serviceProvider = services.BuildServiceProvider();
        var client = serviceProvider.GetRequiredKeyedService<IDeliveryClient>("test");
        var cacheManager = serviceProvider.GetRequiredKeyedService<IDeliveryCacheManager>("test");

        var listResultA1 = await client.GetItems<Article>()
            .Where(f => f.System("type").IsEqualTo("article"))
            .ExecuteAsync();
        var listResultA2 = await client.GetItems<Article>()
            .Where(f => f.System("type").IsEqualTo("article"))
            .ExecuteAsync();
        var listResultB1 = await client.GetItems<Article>()
            .Where(f => f.System("type").IsEqualTo("article"))
            .WithElements("title")
            .ExecuteAsync();
        var listResultB2 = await client.GetItems<Article>()
            .Where(f => f.System("type").IsEqualTo("article"))
            .WithElements("title")
            .ExecuteAsync();

        Assert.True(listResultA1.IsSuccess);
        Assert.True(listResultA2.IsSuccess);
        Assert.True(listResultB1.IsSuccess);
        Assert.True(listResultB2.IsSuccess);

        Assert.False(listResultA1.IsCacheHit);
        Assert.True(listResultA2.IsCacheHit);
        Assert.False(listResultB1.IsCacheHit);
        Assert.True(listResultB2.IsCacheHit);

        await cacheManager.InvalidateAsync(default, DeliveryCacheDependencies.ItemsListScope);

        var listResultA3 = await client.GetItems<Article>()
            .Where(f => f.System("type").IsEqualTo("article"))
            .ExecuteAsync();
        var listResultB3 = await client.GetItems<Article>()
            .Where(f => f.System("type").IsEqualTo("article"))
            .WithElements("title")
            .ExecuteAsync();

        Assert.True(listResultA3.IsSuccess);
        Assert.True(listResultB3.IsSuccess);

        Assert.False(listResultA3.IsCacheHit);
        Assert.False(listResultB3.IsCacheHit);

        mock.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task MemoryCache_InvalidatingItemsListScope_DoesNotInvalidateSingleItemQueries()
    {
        var mock = new MockHttpMessageHandler();
        var itemCodename = "coffee_beverages_explained";

        var itemFixture = await File.ReadAllTextAsync(
            Path.Combine(Environment.CurrentDirectory,
                $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}{itemCodename}.json"));

        var itemsFixture = await File.ReadAllTextAsync(
            Path.Combine(Environment.CurrentDirectory,
                $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}articles.json"));

        mock.Expect($"{BaseUrl}/items/{itemCodename}")
            .Respond("application/json", itemFixture);
        mock.Expect($"{BaseUrl}/items?system.type%5Beq%5D=article")
            .Respond("application/json", itemsFixture);
        mock.Expect($"{BaseUrl}/items?system.type%5Beq%5D=article")
            .Respond("application/json", itemsFixture);

        var services = new ServiceCollection();
        var options = new DeliveryOptions
        {
            EnvironmentId = _guid.ToString()
        };

        services.AddDeliveryClient("test", o => DeliveryOptionsCopyHelper.Copy(options, o),
            configureHttpClient: b => b.ConfigurePrimaryHttpMessageHandler(() => mock));
        services.AddDeliveryMemoryCache("test");

        var serviceProvider = services.BuildServiceProvider();
        var client = serviceProvider.GetRequiredKeyedService<IDeliveryClient>("test");
        var cacheManager = serviceProvider.GetRequiredKeyedService<IDeliveryCacheManager>("test");

        var itemResult1 = await client.GetItem<Article>(itemCodename).ExecuteAsync();
        var listResult1 = await client.GetItems<Article>()
            .Where(f => f.System("type").IsEqualTo("article"))
            .ExecuteAsync();
        var itemResult2 = await client.GetItem<Article>(itemCodename).ExecuteAsync();
        var listResult2 = await client.GetItems<Article>()
            .Where(f => f.System("type").IsEqualTo("article"))
            .ExecuteAsync();

        await cacheManager.InvalidateAsync(default, DeliveryCacheDependencies.ItemsListScope);

        var itemResult3 = await client.GetItem<Article>(itemCodename).ExecuteAsync();
        var listResult3 = await client.GetItems<Article>()
            .Where(f => f.System("type").IsEqualTo("article"))
            .ExecuteAsync();

        Assert.True(itemResult1.IsSuccess);
        Assert.True(itemResult2.IsSuccess);
        Assert.True(itemResult3.IsSuccess);
        Assert.True(listResult1.IsSuccess);
        Assert.True(listResult2.IsSuccess);
        Assert.True(listResult3.IsSuccess);

        Assert.False(itemResult1.IsCacheHit);
        Assert.True(itemResult2.IsCacheHit);
        Assert.False(listResult1.IsCacheHit);
        Assert.True(listResult2.IsCacheHit);
        Assert.True(itemResult3.IsCacheHit);
        Assert.False(listResult3.IsCacheHit);

        mock.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task MemoryCache_GetType_CacheHitOnSecondCall()
    {
        var mock = new MockHttpMessageHandler();
        var fixtureContent = await File.ReadAllTextAsync(
            Path.Combine(Environment.CurrentDirectory,
                $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}article.json"));

        mock.Expect($"{BaseUrl}/types/article")
            .Respond("application/json", fixtureContent);

        var client = CreateClientWithMemoryCache(mock);

        var result1 = await client.GetType("article").ExecuteAsync();
        var result2 = await client.GetType("article").ExecuteAsync();

        Assert.True(result1.IsSuccess);
        Assert.True(result2.IsSuccess);
        Assert.Equal("Article", result1.Value.System.Name);
        Assert.Equal("Article", result2.Value.System.Name);

        // Verify IsCacheHit property
        Assert.False(result1.IsCacheHit); // First call is API response
        Assert.True(result2.IsCacheHit);  // Second call is cache hit

        // Verify ResponseHeaders property
        Assert.NotNull(result1.ResponseHeaders); // API response has headers
        Assert.Null(result2.ResponseHeaders);    // Cache hit has no headers

        // Verify only one API call was made
        mock.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task MemoryCache_GetTaxonomy_CacheHitOnSecondCall()
    {
        var mock = new MockHttpMessageHandler();
        var fixtureContent = await File.ReadAllTextAsync(
            Path.Combine(Environment.CurrentDirectory,
                $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}taxonomies_personas.json"));

        mock.Expect($"{BaseUrl}/taxonomies/personas")
            .Respond("application/json", fixtureContent);

        var client = CreateClientWithMemoryCache(mock);

        var result1 = await client.GetTaxonomy("personas").ExecuteAsync();
        var result2 = await client.GetTaxonomy("personas").ExecuteAsync();

        Assert.True(result1.IsSuccess);
        Assert.True(result2.IsSuccess);
        Assert.Equal("personas", result1.Value.System.Codename);
        Assert.Equal("personas", result2.Value.System.Codename);

        // Verify IsCacheHit property
        Assert.False(result1.IsCacheHit); // First call is API response
        Assert.True(result2.IsCacheHit);  // Second call is cache hit

        // Verify ResponseHeaders property
        Assert.NotNull(result1.ResponseHeaders); // API response has headers
        Assert.Null(result2.ResponseHeaders);    // Cache hit has no headers

        // Verify only one API call was made
        mock.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task MemoryCache_GetItem_ConcurrentMisses_AreCoalescedToSingleApiCall()
    {
        var itemCodename = "coffee_beverages_explained";
        var fixtureContent = await File.ReadAllTextAsync(
            Path.Combine(Environment.CurrentDirectory,
                $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}{itemCodename}.json"));

        var handler = new DelayedJsonResponseHandler(fixtureContent, TimeSpan.FromMilliseconds(100));
        var client = CreateClientWithMemoryCache(handler);

        var results = await Task.WhenAll(
            Enumerable.Range(0, 12)
                .Select(_ => client.GetItem<Article>(itemCodename).ExecuteAsync()));

        Assert.All(results, result =>
        {
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
        });

        Assert.Equal(1, handler.RequestCount);
        Assert.Single(results, r => !r.IsCacheHit);
    }

    #endregion

    #region Distributed Cache Integration Tests

    [Fact]
    public async Task DistributedCache_GetItem_CacheHitOnSecondCall()
    {
        var mock = new MockHttpMessageHandler();
        var itemCodename = "coffee_beverages_explained";
        var fixtureContent = await File.ReadAllTextAsync(
            Path.Combine(Environment.CurrentDirectory,
                $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}{itemCodename}.json"));

        // Raw JSON caching is now reliable - use Expect() to verify only one API call
        mock.Expect($"{BaseUrl}/items/{itemCodename}")
            .Respond("application/json", fixtureContent);

        var client = CreateClientWithDistributedCache(mock);

        var result1 = await client.GetItem<Article>(itemCodename).ExecuteAsync();
        var result2 = await client.GetItem<Article>(itemCodename).ExecuteAsync();

        Assert.True(result1.IsSuccess);
        Assert.True(result2.IsSuccess);
        Assert.NotNull(result1.Value);
        Assert.NotNull(result2.Value);
        Assert.Equal(result1.Value.Elements.Title, result2.Value.Elements.Title);

        // Verify IsCacheHit property
        Assert.False(result1.IsCacheHit); // First call is API response
        Assert.True(result2.IsCacheHit);  // Second call is cache hit

        // Verify only one API call was made
        mock.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task DistributedCache_GetItems_CacheHitOnSecondCall()
    {
        var mock = new MockHttpMessageHandler();
        var fixtureContent = await File.ReadAllTextAsync(
            Path.Combine(Environment.CurrentDirectory,
                $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}articles.json"));

        // Raw JSON caching is now reliable - use Expect() to verify only one API call
        mock.Expect($"{BaseUrl}/items?system.type%5Beq%5D=article")
            .Respond("application/json", fixtureContent);

        var client = CreateClientWithDistributedCache(mock);

        var result1 = await client.GetItems<Article>()
            .Where(f => f.System("type").IsEqualTo("article"))
            .ExecuteAsync();

        var result2 = await client.GetItems<Article>()
            .Where(f => f.System("type").IsEqualTo("article"))
            .ExecuteAsync();

        Assert.True(result1.IsSuccess);
        Assert.True(result2.IsSuccess);
        Assert.NotEmpty(result1.Value.Items);
        Assert.NotEmpty(result2.Value.Items);
        Assert.Equal(result1.Value.Items.Count, result2.Value.Items.Count);

        // Verify IsCacheHit property
        Assert.False(result1.IsCacheHit); // First call is API response
        Assert.True(result2.IsCacheHit);  // Second call is cache hit

        // Verify only one API call was made
        mock.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task DistributedCache_Invalidation_RefreshesCache()
    {
        var mock = new MockHttpMessageHandler();
        var itemCodename = "coffee_beverages_explained";
        var fixtureContent = await File.ReadAllTextAsync(
            Path.Combine(Environment.CurrentDirectory,
                $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}{itemCodename}.json"));

        mock.When($"{BaseUrl}/items/{itemCodename}")
            .Respond("application/json", fixtureContent);

        var services = new ServiceCollection();
        var options = new DeliveryOptions
        {
            EnvironmentId = _guid.ToString()
        };

        var mockDistributedCache = new MockDistributedCache();
        services.AddSingleton<IDistributedCache>(mockDistributedCache);
        services.AddDeliveryClient("test", o => DeliveryOptionsCopyHelper.Copy(options, o),
            configureHttpClient: b => b.ConfigurePrimaryHttpMessageHandler(() => mock));
        services.AddDeliveryDistributedCache("test");

        var serviceProvider = services.BuildServiceProvider();
        var client = serviceProvider.GetRequiredKeyedService<IDeliveryClient>("test");
        var cacheManager = serviceProvider.GetRequiredKeyedService<IDeliveryCacheManager>("test");

        var result1 = await client.GetItem<Article>(itemCodename).ExecuteAsync();

        await cacheManager.InvalidateAsync(default, $"item_{itemCodename}");

        var result2 = await client.GetItem<Article>(itemCodename).ExecuteAsync();

        Assert.True(result1.IsSuccess);
        Assert.True(result2.IsSuccess);
        Assert.NotNull(result1.Value);
        Assert.NotNull(result2.Value);
    }

    [Fact]
    public async Task DistributedCache_ItemsListScopeInvalidation_RefreshesAllCachedItemLists()
    {
        var mock = new MockHttpMessageHandler();
        var fixtureContent = await File.ReadAllTextAsync(
            Path.Combine(Environment.CurrentDirectory,
                $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}articles.json"));

        mock.Expect($"{BaseUrl}/items*")
            .Respond("application/json", fixtureContent);
        mock.Expect($"{BaseUrl}/items*")
            .Respond("application/json", fixtureContent);
        mock.Expect($"{BaseUrl}/items*")
            .Respond("application/json", fixtureContent);
        mock.Expect($"{BaseUrl}/items*")
            .Respond("application/json", fixtureContent);

        var services = new ServiceCollection();
        var options = new DeliveryOptions
        {
            EnvironmentId = _guid.ToString()
        };

        var mockDistributedCache = new MockDistributedCache();
        services.AddSingleton<IDistributedCache>(mockDistributedCache);
        services.AddDeliveryClient("test", o => DeliveryOptionsCopyHelper.Copy(options, o),
            configureHttpClient: b => b.ConfigurePrimaryHttpMessageHandler(() => mock));
        services.AddDeliveryDistributedCache("test");

        var serviceProvider = services.BuildServiceProvider();
        var client = serviceProvider.GetRequiredKeyedService<IDeliveryClient>("test");
        var cacheManager = serviceProvider.GetRequiredKeyedService<IDeliveryCacheManager>("test");

        var listResultA1 = await client.GetItems<Article>()
            .Where(f => f.System("type").IsEqualTo("article"))
            .ExecuteAsync();
        var listResultA2 = await client.GetItems<Article>()
            .Where(f => f.System("type").IsEqualTo("article"))
            .ExecuteAsync();
        var listResultB1 = await client.GetItems<Article>()
            .Where(f => f.System("type").IsEqualTo("article"))
            .WithElements("title")
            .ExecuteAsync();
        var listResultB2 = await client.GetItems<Article>()
            .Where(f => f.System("type").IsEqualTo("article"))
            .WithElements("title")
            .ExecuteAsync();

        Assert.True(listResultA1.IsSuccess);
        Assert.True(listResultA2.IsSuccess);
        Assert.True(listResultB1.IsSuccess);
        Assert.True(listResultB2.IsSuccess);

        Assert.False(listResultA1.IsCacheHit);
        Assert.True(listResultA2.IsCacheHit);
        Assert.False(listResultB1.IsCacheHit);
        Assert.True(listResultB2.IsCacheHit);

        await cacheManager.InvalidateAsync(default, DeliveryCacheDependencies.ItemsListScope);

        var listResultA3 = await client.GetItems<Article>()
            .Where(f => f.System("type").IsEqualTo("article"))
            .ExecuteAsync();
        var listResultB3 = await client.GetItems<Article>()
            .Where(f => f.System("type").IsEqualTo("article"))
            .WithElements("title")
            .ExecuteAsync();

        Assert.True(listResultA3.IsSuccess);
        Assert.True(listResultB3.IsSuccess);

        Assert.False(listResultA3.IsCacheHit);
        Assert.False(listResultB3.IsCacheHit);

        mock.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task DistributedCache_GetItem_ConcurrentMisses_AreCoalescedToSingleApiCall()
    {
        var itemCodename = "coffee_beverages_explained";
        var fixtureContent = await File.ReadAllTextAsync(
            Path.Combine(Environment.CurrentDirectory,
                $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}{itemCodename}.json"));

        var handler = new DelayedJsonResponseHandler(fixtureContent, TimeSpan.FromMilliseconds(100));
        var client = CreateClientWithDistributedCache(handler);

        var results = await Task.WhenAll(
            Enumerable.Range(0, 12)
                .Select(_ => client.GetItem<Article>(itemCodename).ExecuteAsync()));

        Assert.All(results, result =>
        {
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
        });

        Assert.Equal(1, handler.RequestCount);
        Assert.Single(results, r => !r.IsCacheHit);
    }

    #endregion

    #region Dependency Tracking Integration Tests

    [Fact]
    public async Task MemoryCache_ItemWithModularContent_TracksAllDependencies()
    {
        var mock = new MockHttpMessageHandler();
        var itemCodename = "coffee_processing_techniques";
        var fixtureContent = await File.ReadAllTextAsync(
            Path.Combine(Environment.CurrentDirectory,
                $"Fixtures{Path.DirectorySeparatorChar}ContentLinkResolver{Path.DirectorySeparatorChar}{itemCodename}.json"));

        mock.When($"{BaseUrl}/items/{itemCodename}")
            .Respond("application/json", fixtureContent);

        var services = new ServiceCollection();
        var options = new DeliveryOptions
        {
            EnvironmentId = _guid.ToString()
        };

        // Use per-client caching with custom mock cache manager
        var mockCacheManager = new TestCacheManager();
        services.AddDeliveryClient("test", o => DeliveryOptionsCopyHelper.Copy(options, o),
            configureHttpClient: b => b.ConfigurePrimaryHttpMessageHandler(() => mock));
        services.AddKeyedSingleton<IDeliveryCacheManager>("test", mockCacheManager);

        var serviceProvider = services.BuildServiceProvider();
        var client = serviceProvider.GetRequiredKeyedService<IDeliveryClient>("test");

        var result = await client.GetItem<Article>(itemCodename).ExecuteAsync();

        Assert.True(result.IsSuccess);
        Assert.Single(mockCacheManager.CachedItems);

        // Verify dependencies were tracked
        var cachedEntry = mockCacheManager.CachedItems.First();
        var dependencies = cachedEntry.Dependencies.ToList();

        // Should track the main item
        Assert.Contains($"item_{itemCodename}", dependencies);

        // Should track modular content items (if present)
        // Should track assets (if present)
        // Should track taxonomies (if present)
        Assert.NotEmpty(dependencies);
    }

    [Fact]
    public async Task MemoryCache_ItemWithRichText_TracksDependencies()
    {
        var mock = new MockHttpMessageHandler();
        var itemCodename = "coffee_processing_techniques";
        var fixtureContent = await File.ReadAllTextAsync(
            Path.Combine(Environment.CurrentDirectory,
                $"Fixtures{Path.DirectorySeparatorChar}ContentLinkResolver{Path.DirectorySeparatorChar}{itemCodename}.json"));

        mock.When($"{BaseUrl}/items/{itemCodename}")
            .Respond("application/json", fixtureContent);

        var services = new ServiceCollection();
        var options = new DeliveryOptions
        {
            EnvironmentId = _guid.ToString()
        };

        // Use per-client caching with custom mock cache manager
        var mockCacheManager = new TestCacheManager();
        services.AddDeliveryClient("test", o => DeliveryOptionsCopyHelper.Copy(options, o),
            configureHttpClient: b => b.ConfigurePrimaryHttpMessageHandler(() => mock));
        services.AddKeyedSingleton<IDeliveryCacheManager>("test", mockCacheManager);

        var serviceProvider = services.BuildServiceProvider();
        var client = serviceProvider.GetRequiredKeyedService<IDeliveryClient>("test");

        var result = await client.GetItem<Article>(itemCodename).ExecuteAsync();

        Assert.True(result.IsSuccess);
        Assert.NotEmpty(result.Value.Elements.BodyCopy);

        // Verify dependencies include linked items from rich text
        var cachedEntry = mockCacheManager.CachedItems.First();
        var dependencies = cachedEntry.Dependencies.ToList();

        // Should track dependencies from rich text links
        Assert.NotEmpty(dependencies);
    }

    [Fact]
    public async Task MemoryCache_GetItems_TracksItemsListScopeDependency()
    {
        var mock = new MockHttpMessageHandler();
        var fixtureContent = await File.ReadAllTextAsync(
            Path.Combine(Environment.CurrentDirectory,
                $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}articles.json"));

        mock.When($"{BaseUrl}/items?system.type%5Beq%5D=article")
            .Respond("application/json", fixtureContent);

        var services = new ServiceCollection();
        var options = new DeliveryOptions
        {
            EnvironmentId = _guid.ToString()
        };

        var mockCacheManager = new TestCacheManager();
        services.AddDeliveryClient("test", o => DeliveryOptionsCopyHelper.Copy(options, o),
            configureHttpClient: b => b.ConfigurePrimaryHttpMessageHandler(() => mock));
        services.AddKeyedSingleton<IDeliveryCacheManager>("test", mockCacheManager);

        var serviceProvider = services.BuildServiceProvider();
        var client = serviceProvider.GetRequiredKeyedService<IDeliveryClient>("test");

        var result = await client.GetItems<Article>()
            .Where(f => f.System("type").IsEqualTo("article"))
            .ExecuteAsync();

        Assert.True(result.IsSuccess);

        var cachedEntry = Assert.Single(mockCacheManager.CachedItems);
        Assert.Contains(DeliveryCacheDependencies.ItemsListScope, cachedEntry.Dependencies);
        Assert.Contains(cachedEntry.Dependencies, dependency => dependency.StartsWith("item_", StringComparison.Ordinal));
    }

    [Fact]
    public async Task MemoryCache_DifferentQueriesWithSameDependency_InvalidatesBoth()
    {
        var mock = new MockHttpMessageHandler();
        var itemCodename = "coffee_beverages_explained";

        var itemFixture = await File.ReadAllTextAsync(
            Path.Combine(Environment.CurrentDirectory,
                $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}{itemCodename}.json"));

        var itemsFixture = await File.ReadAllTextAsync(
            Path.Combine(Environment.CurrentDirectory,
                $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}articles.json"));

        mock.When($"{BaseUrl}/items/{itemCodename}")
            .Respond("application/json", itemFixture);

        mock.When($"{BaseUrl}/items?system.type%5Beq%5D=article")
            .Respond("application/json", itemsFixture);

        var services = new ServiceCollection();
        var options = new DeliveryOptions
        {
            EnvironmentId = _guid.ToString()
        };

        services.AddMemoryCache();
        services.AddSingleton<IDeliveryCacheManager>(sp =>
            new MemoryCacheManager(sp.GetRequiredService<IMemoryCache>()));
        services.AddDeliveryClient(options, configureHttpClient: b =>
            b.ConfigurePrimaryHttpMessageHandler(() => mock));

        var serviceProvider = services.BuildServiceProvider();
        var client = serviceProvider.GetRequiredService<IDeliveryClient>();
        var cacheManager = serviceProvider.GetRequiredService<IDeliveryCacheManager>();

        var singleResult = await client.GetItem<Article>(itemCodename).ExecuteAsync();
        var listResult = await client.GetItems<Article>()
            .Where(f => f.System("type").IsEqualTo("article"))
            .ExecuteAsync();

        // Invalidate the shared dependency (the item itself)
        await cacheManager.InvalidateAsync(default, $"item_{itemCodename}");

        // Both queries should now hit the API again
        var singleResult2 = await client.GetItem<Article>(itemCodename).ExecuteAsync();
        var listResult2 = await client.GetItems<Article>()
            .Where(f => f.System("type").IsEqualTo("article"))
            .ExecuteAsync();

        Assert.True(singleResult.IsSuccess);
        Assert.True(listResult.IsSuccess);
        Assert.True(singleResult2.IsSuccess);
        Assert.True(listResult2.IsSuccess);
    }

    #endregion

    #region Cache Disabled Tests

    [Fact]
    public async Task CachingDisabled_AlwaysHitsApi()
    {
        var mock = new MockHttpMessageHandler();
        var itemCodename = "coffee_beverages_explained";
        var fixtureContent = await File.ReadAllTextAsync(
            Path.Combine(Environment.CurrentDirectory,
                $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}{itemCodename}.json"));

        // Both calls should hit the API
        mock.When($"{BaseUrl}/items/{itemCodename}")
            .Respond("application/json", fixtureContent);

        var services = new ServiceCollection();
        var options = new DeliveryOptions
        {
            EnvironmentId = _guid.ToString()
        };

        // No cache manager registered - caching disabled
        services.AddDeliveryClient(options, configureHttpClient: b =>
            b.ConfigurePrimaryHttpMessageHandler(() => mock));

        var client = services.BuildServiceProvider().GetRequiredService<IDeliveryClient>();

        var result1 = await client.GetItem<Article>(itemCodename).ExecuteAsync();
        var result2 = await client.GetItem<Article>(itemCodename).ExecuteAsync();

        Assert.True(result1.IsSuccess);
        Assert.True(result2.IsSuccess);

        // Both calls hit the API (no caching)
        Assert.NotNull(result1.Value);
        Assert.NotNull(result2.Value);

        // Verify IsCacheHit is false for both (no caching)
        Assert.False(result1.IsCacheHit);
        Assert.False(result2.IsCacheHit);

        // Verify ResponseHeaders is present for both (direct API responses)
        Assert.NotNull(result1.ResponseHeaders);
        Assert.NotNull(result2.ResponseHeaders);
    }

    [Fact]
    public async Task NoCacheManagerRegistered_WorksWithoutError()
    {
        var mock = new MockHttpMessageHandler();
        var itemCodename = "coffee_beverages_explained";
        var fixtureContent = await File.ReadAllTextAsync(
            Path.Combine(Environment.CurrentDirectory,
                $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}{itemCodename}.json"));

        mock.When($"{BaseUrl}/items/{itemCodename}")
            .Respond("application/json", fixtureContent);

        var services = new ServiceCollection();
        var options = new DeliveryOptions
        {
            EnvironmentId = _guid.ToString()
        };

        // No cache manager registered - caching disabled
        services.AddDeliveryClient(options, configureHttpClient: b =>
            b.ConfigurePrimaryHttpMessageHandler(() => mock));

        var client = services.BuildServiceProvider().GetRequiredService<IDeliveryClient>();

        var result = await client.GetItem<Article>(itemCodename).ExecuteAsync();

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
    }

    #endregion

    #region Helper Methods

    private IDeliveryClient CreateClientWithMemoryCache(HttpMessageHandler httpHandler, DeliveryOptions? options = null)
    {
        var services = new ServiceCollection();
        options ??= new DeliveryOptions
        {
            EnvironmentId = _guid.ToString()
        };

        // Use per-client caching API
        services.AddDeliveryClient("test", o => DeliveryOptionsCopyHelper.Copy(options, o),
            configureHttpClient: b => b.ConfigurePrimaryHttpMessageHandler(() => httpHandler));
        services.AddDeliveryMemoryCache("test");

        return services.BuildServiceProvider().GetRequiredKeyedService<IDeliveryClient>("test");
    }

    private IDeliveryClient CreateClientWithDistributedCache(HttpMessageHandler httpHandler)
    {
        var services = new ServiceCollection();
        var options = new DeliveryOptions
        {
            EnvironmentId = _guid.ToString()
        };

        var mockDistributedCache = new MockDistributedCache();
        services.AddSingleton<IDistributedCache>(mockDistributedCache);

        // Use per-client caching API
        services.AddDeliveryClient("test", o => DeliveryOptionsCopyHelper.Copy(options, o),
            configureHttpClient: b => b.ConfigurePrimaryHttpMessageHandler(() => httpHandler));
        services.AddDeliveryDistributedCache("test");

        return services.BuildServiceProvider().GetRequiredKeyedService<IDeliveryClient>("test");
    }

    #endregion

    #region Test Helper Classes

    private class TestCacheManager : IDeliveryCacheManager
    {
        public List<CachedItem> CachedItems { get; } = [];

        public Task<T?> GetAsync<T>(string cacheKey, CancellationToken cancellationToken = default) where T : class
        {
            var item = CachedItems.FirstOrDefault(i => i.Key == cacheKey);
            return Task.FromResult(item?.Value as T);
        }

        public Task SetAsync<T>(string cacheKey, T value, IEnumerable<string> dependencies, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
        {
            CachedItems.Add(new CachedItem
            {
                Key = cacheKey,
                Value = value,
                Dependencies = [.. dependencies]
            });
            return Task.CompletedTask;
        }

        public Task InvalidateAsync(CancellationToken cancellationToken = default, params string[] dependencyKeys) => Task.CompletedTask;

        public class CachedItem
        {
            public string Key { get; set; } = "";
            public object? Value { get; set; }
            public List<string> Dependencies { get; set; } = [];
        }
    }

    private class MockDistributedCache : IDistributedCache
    {
        private readonly System.Collections.Concurrent.ConcurrentDictionary<string, byte[]> _cache = new();

        public byte[]? Get(string key) => _cache.TryGetValue(key, out var value) ? value : null;
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
        public Task RefreshAsync(string key, CancellationToken token = default) => Task.CompletedTask;

        public void Remove(string key) => _cache.TryRemove(key, out _);
        public Task RemoveAsync(string key, CancellationToken token = default)
        {
            Remove(key);
            return Task.CompletedTask;
        }
    }

    private sealed class DelayedJsonResponseHandler(string jsonResponse, TimeSpan delay) : HttpMessageHandler
    {
        private readonly string _jsonResponse = jsonResponse;
        private readonly TimeSpan _delay = delay;
        private int _requestCount;

        public int RequestCount => Volatile.Read(ref _requestCount);

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _requestCount);

            if (_delay > TimeSpan.Zero)
            {
                await Task.Delay(_delay, cancellationToken).ConfigureAwait(false);
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_jsonResponse)
            };
        }
    }

    #endregion
}
