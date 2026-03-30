using System.Net;
using System.Text;
using System.Text.Json;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.Api.QueryParams.Items;
using Kontent.Ai.Delivery.Caching;
using Kontent.Ai.Delivery.Extensions;
using Kontent.Ai.Delivery.Tests.Models.ContentTypes;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RichardSzalay.MockHttp;

namespace Kontent.Ai.Delivery.Tests.Caching;

/// <summary>
/// Integration tests for end-to-end caching scenarios with DeliveryClient.
/// Tests the complete flow: API call → caching → cache hit → invalidation.
/// </summary>
public partial class CachingIntegrationTests
{
    private readonly Guid _guid = Guid.NewGuid();
    private string BaseUrl => $"https://deliver.kontent.ai/{_guid}";
    private string PreviewBaseUrl => $"https://preview-deliver.kontent.ai/{_guid}";

    #region Memory Cache Integration Tests

    [Fact]
    public async Task MemoryCache_GetItem_CacheHitOnSecondCall()
    {
        var mock = new MockHttpMessageHandler();
        var itemCodename = "coffee_beverages_explained";
        var fixtureContent = await ReadFixtureAsync($"DeliveryClient{Path.DirectorySeparatorChar}{itemCodename}.json");

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

        // Verify ResponseSource property
        Assert.Equal(ResponseSource.Origin, result1.ResponseSource);
        Assert.Equal(ResponseSource.Cache, result2.ResponseSource);

        // Verify ResponseHeaders property
        Assert.NotNull(result1.ResponseHeaders); // API response has headers
        Assert.Null(result2.ResponseHeaders);    // Cache hit has no headers

        // Verify only one API call was made
        mock.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task MemoryCache_GetItem_PreservesDependencyKeys_OnCacheHit()
    {
        var mock = new MockHttpMessageHandler();
        var itemCodename = "coffee_beverages_explained";
        var fixtureContent = await ReadFixtureAsync($"DeliveryClient{Path.DirectorySeparatorChar}{itemCodename}.json");

        mock.Expect($"{BaseUrl}/items/{itemCodename}")
            .Respond("application/json", fixtureContent);

        var client = CreateClientWithMemoryCache(mock);

        var result1 = await client.GetItem<Article>(itemCodename).ExecuteAsync();
        var result2 = await client.GetItem<Article>(itemCodename).ExecuteAsync();

        Assert.NotNull(result1.DependencyKeys);
        Assert.NotNull(result2.DependencyKeys);
        var dependencyKeys1 = result1.DependencyKeys;
        var dependencyKeys2 = result2.DependencyKeys;

        Assert.False(result1.IsCacheHit);
        Assert.True(result2.IsCacheHit);

        Assert.Equal(
            dependencyKeys1.OrderBy(key => key, StringComparer.Ordinal).ToArray(),
            dependencyKeys2.OrderBy(key => key, StringComparer.Ordinal).ToArray());
        Assert.Contains("item_coffee_beverages_explained", dependencyKeys2);
        Assert.Contains("item_americano", dependencyKeys2);
        Assert.Contains("item_how_to_make_a_cappuccino", dependencyKeys2);
        Assert.Contains("taxonomy_personas", dependencyKeys2);
        Assert.Contains(dependencyKeys2, key => key.StartsWith("asset_", StringComparison.Ordinal));

        mock.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task MemoryCache_GetItems_PreservesListScopeAndDependencyKeys_OnCacheHit()
    {
        var mock = new MockHttpMessageHandler();
        var fixtureContent = await ReadFixtureAsync($"DeliveryClient{Path.DirectorySeparatorChar}items.json");

        mock.Expect($"{BaseUrl}/items")
            .Respond("application/json", fixtureContent);

        var client = CreateClientWithMemoryCache(mock);

        var result1 = await client.GetItems<object>().ExecuteAsync();
        var result2 = await client.GetItems<object>().ExecuteAsync();

        Assert.False(result1.IsCacheHit);
        Assert.True(result2.IsCacheHit);

        Assert.NotNull(result1.DependencyKeys);
        Assert.NotNull(result2.DependencyKeys);
        Assert.Contains(DeliveryCacheDependencies.ItemsListScope, result2.DependencyKeys);
        Assert.Contains("item_article_1", result2.DependencyKeys);

        Assert.Equal(
            result1.DependencyKeys.OrderBy(k => k, StringComparer.Ordinal).ToArray(),
            result2.DependencyKeys.OrderBy(k => k, StringComparer.Ordinal).ToArray());

        mock.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task MemoryCache_FailSafe_PreservesDependencyKeys()
    {
        var mock = new MockHttpMessageHandler();
        var itemCodename = "coffee_beverages_explained";
        var fixtureContent = await ReadFixtureAsync($"DeliveryClient{Path.DirectorySeparatorChar}{itemCodename}.json");

        var options = new DeliveryOptions
        {
            EnvironmentId = _guid.ToString()
        };

        var services = new ServiceCollection();
        AddNamedDeliveryClient(services, "test", options, mock);
        services.AddDeliveryMemoryCache("test", opts =>
        {
            opts.DefaultExpiration = TimeSpan.FromMilliseconds(50);
            opts.IsFailSafeEnabled = true;
            opts.FailSafeMaxDuration = TimeSpan.FromMinutes(5);
            opts.FailSafeThrottleDuration = TimeSpan.FromSeconds(1);
        });
        var serviceProvider = services.BuildServiceProvider();
        var client = serviceProvider.GetRequiredKeyedService<IDeliveryClient>("test");

        // First call populates the cache
        mock.Expect($"{BaseUrl}/items/{itemCodename}")
            .Respond("application/json", fixtureContent);

        var result1 = await client.GetItem<Article>(itemCodename).ExecuteAsync();
        Assert.True(result1.IsSuccess);
        Assert.NotNull(result1.DependencyKeys);
        var originalKeys = result1.DependencyKeys;

        // Wait for the cache entry to expire
        await Task.Delay(200);

        // Second call: API returns 500, fail-safe should serve stale data with original keys
        mock.Expect($"{BaseUrl}/items/{itemCodename}")
            .Respond(HttpStatusCode.InternalServerError, "application/json", """{"message":"Server error","error_code":500}""");

        var result2 = await client.GetItem<Article>(itemCodename).ExecuteAsync();

        Assert.True(result2.IsSuccess);
        Assert.Equal(ResponseSource.FailSafe, result2.ResponseSource);
        Assert.NotNull(result2.DependencyKeys);
        Assert.Contains("item_coffee_beverages_explained", result2.DependencyKeys);

        Assert.Equal(
            originalKeys.OrderBy(k => k, StringComparer.Ordinal).ToArray(),
            result2.DependencyKeys.OrderBy(k => k, StringComparer.Ordinal).ToArray());

        mock.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task MemoryCache_GetItem_ExpiresAfterTtl_HitsApiAgain()
    {
        var mock = new MockHttpMessageHandler();
        var itemCodename = "coffee_beverages_explained";
        var fixtureContent = await ReadFixtureAsync($"DeliveryClient{Path.DirectorySeparatorChar}{itemCodename}.json");

        var options = new DeliveryOptions
        {
            EnvironmentId = _guid.ToString(),
            EnableResilience = false
        };

        var serviceProvider = BuildNamedMemoryCacheServiceProvider(
            mock,
            options,
            defaultExpiration: TimeSpan.FromMilliseconds(50));
        var client = serviceProvider.GetRequiredKeyedService<IDeliveryClient>("test");

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
    public async Task MemoryCache_GetItem_WithPerQueryCacheExpiration_UsesQuerySpecificTtl()
    {
        var mock = new MockHttpMessageHandler();
        var itemCodename = "coffee_beverages_explained";
        var fixtureContent = await ReadFixtureAsync($"DeliveryClient{Path.DirectorySeparatorChar}{itemCodename}.json");

        var options = new DeliveryOptions
        {
            EnvironmentId = _guid.ToString(),
            EnableResilience = false
        };

        var serviceProvider = BuildNamedMemoryCacheServiceProvider(
            mock,
            options,
            defaultExpiration: TimeSpan.FromMinutes(10));
        var client = serviceProvider.GetRequiredKeyedService<IDeliveryClient>("test");

        mock.Expect($"{BaseUrl}/items/{itemCodename}")
            .Respond("application/json", fixtureContent);

        var result1 = await client.GetItem<Article>(itemCodename)
            .WithCacheExpiration(TimeSpan.FromMilliseconds(50))
            .ExecuteAsync();
        var result2 = await client.GetItem<Article>(itemCodename)
            .WithCacheExpiration(TimeSpan.FromMilliseconds(50))
            .ExecuteAsync();

        Assert.False(result1.IsCacheHit);
        Assert.True(result2.IsCacheHit);
        mock.VerifyNoOutstandingExpectation();

        await Task.Delay(200);

        mock.Expect($"{BaseUrl}/items/{itemCodename}")
            .Respond("application/json", fixtureContent);

        var result3 = await client.GetItem<Article>(itemCodename)
            .WithCacheExpiration(TimeSpan.FromMilliseconds(50))
            .ExecuteAsync();

        Assert.False(result3.IsCacheHit);
        mock.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task MemoryCache_GetItem_QueryWaitEnabled_BypassesCache()
    {
        var mock = new MockHttpMessageHandler();
        var itemCodename = "coffee_beverages_explained";
        var fixtureContent = await ReadFixtureAsync($"DeliveryClient{Path.DirectorySeparatorChar}{itemCodename}.json");

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
        var fixtureContent = await ReadFixtureAsync($"DeliveryClient{Path.DirectorySeparatorChar}{itemCodename}.json");

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
    public async Task MemoryCache_PreviewClient_BypassesCacheEvenWhenRegistered()
    {
        var mock = new MockHttpMessageHandler();
        var itemCodename = "coffee_beverages_explained";
        var fixtureContent = await ReadFixtureAsync($"DeliveryClient{Path.DirectorySeparatorChar}{itemCodename}.json");

        mock.Expect($"{PreviewBaseUrl}/items/{itemCodename}")
            .Respond("application/json", fixtureContent);
        mock.Expect($"{PreviewBaseUrl}/items/{itemCodename}")
            .Respond("application/json", fixtureContent);

        var client = CreateClientWithMemoryCache(
            mock,
            new DeliveryOptions
            {
                EnvironmentId = _guid.ToString(),
                UsePreviewApi = true,
                PreviewApiKey = "preview.api.key"
            });

        var result1 = await client.GetItem<Article>(itemCodename).ExecuteAsync();
        var result2 = await client.GetItem<Article>(itemCodename).ExecuteAsync();

        Assert.True(result1.IsSuccess);
        Assert.True(result2.IsSuccess);
        Assert.False(result1.IsCacheHit);
        Assert.False(result2.IsCacheHit);
        mock.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task MemoryCache_ProductionAndPreviewClients_ProductionCachesPreviewBypasses()
    {
        var mock = new MockHttpMessageHandler();
        var itemCodename = "coffee_beverages_explained";
        var fixtureContent = await ReadFixtureAsync($"DeliveryClient{Path.DirectorySeparatorChar}{itemCodename}.json");

        // Production should cache (single API call for two requests).
        mock.Expect($"{BaseUrl}/items/{itemCodename}")
            .Respond("application/json", fixtureContent);

        // Preview should always bypass cache (two API calls for two requests).
        mock.Expect($"{PreviewBaseUrl}/items/{itemCodename}")
            .Respond("application/json", fixtureContent);
        mock.Expect($"{PreviewBaseUrl}/items/{itemCodename}")
            .Respond("application/json", fixtureContent);

        var services = new ServiceCollection();
        services.AddDeliveryClient("production", o =>
        {
            o.EnvironmentId = _guid.ToString();
        }, configureHttpClient: b => b.ConfigurePrimaryHttpMessageHandler(() => mock));
        services.AddDeliveryClient("preview", o =>
        {
            o.EnvironmentId = _guid.ToString();
            o.UsePreviewApi = true;
            o.PreviewApiKey = "preview.api.key";
        }, configureHttpClient: b => b.ConfigurePrimaryHttpMessageHandler(() => mock));

        services.AddDeliveryMemoryCache("production");
        services.AddDeliveryMemoryCache("preview");

        var serviceProvider = services.BuildServiceProvider();
        var productionClient = serviceProvider.GetRequiredKeyedService<IDeliveryClient>("production");
        var previewClient = serviceProvider.GetRequiredKeyedService<IDeliveryClient>("preview");

        var productionResult1 = await productionClient.GetItem<Article>(itemCodename).ExecuteAsync();
        var productionResult2 = await productionClient.GetItem<Article>(itemCodename).ExecuteAsync();
        var previewResult1 = await previewClient.GetItem<Article>(itemCodename).ExecuteAsync();
        var previewResult2 = await previewClient.GetItem<Article>(itemCodename).ExecuteAsync();

        Assert.True(productionResult1.IsSuccess);
        Assert.True(productionResult2.IsSuccess);
        Assert.True(previewResult1.IsSuccess);
        Assert.True(previewResult2.IsSuccess);
        Assert.False(productionResult1.IsCacheHit);
        Assert.True(productionResult2.IsCacheHit);
        Assert.False(previewResult1.IsCacheHit);
        Assert.False(previewResult2.IsCacheHit);
        mock.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task MemoryCache_RuntimeEnvironmentIdChange_UsesExistingCacheUntilPurged()
    {
        var initialEnvironmentId = _guid.ToString();
        var updatedEnvironmentId = Guid.NewGuid().ToString();
        var itemCodename = "coffee_beverages_explained";
        var clientName = "test";

        var fixtureContent = await ReadFixtureAsync($"DeliveryClient{Path.DirectorySeparatorChar}{itemCodename}.json");

        var mock = new MockHttpMessageHandler();
        mock.Expect($"https://deliver.kontent.ai/{initialEnvironmentId}/items/{itemCodename}")
            .Respond("application/json", fixtureContent);
        mock.Expect($"https://deliver.kontent.ai/{updatedEnvironmentId}/items/{itemCodename}")
            .Respond("application/json", fixtureContent);

        var mutableOptions = new DeliveryOptions
        {
            EnvironmentId = initialEnvironmentId
        };

        var serviceProvider = BuildNamedMemoryCacheServiceProvider(mock, mutableOptions, clientName);
        var client = serviceProvider.GetRequiredKeyedService<IDeliveryClient>(clientName);
        var optionsCache = serviceProvider.GetRequiredService<IOptionsMonitorCache<DeliveryOptions>>();
        var cacheManager = serviceProvider.GetRequiredKeyedService<IDeliveryCacheManager>(clientName);

        var result1 = await client.GetItem<Article>(itemCodename).ExecuteAsync();

        mutableOptions.EnvironmentId = updatedEnvironmentId;
        optionsCache.TryRemove(clientName);

        var result2 = await client.GetItem<Article>(itemCodename).ExecuteAsync();

        var purger = Assert.IsAssignableFrom<IDeliveryCachePurger>(cacheManager);
        await purger.PurgeAsync();

        var result3 = await client.GetItem<Article>(itemCodename).ExecuteAsync();

        Assert.True(result1.IsSuccess);
        Assert.True(result2.IsSuccess);
        Assert.True(result3.IsSuccess);
        Assert.False(result1.IsCacheHit);
        Assert.True(result2.IsCacheHit);
        Assert.False(result3.IsCacheHit);
        mock.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task MemoryCache_RuntimeDefaultRenditionPresetChange_UsesExistingCacheUntilPurged()
    {
        var itemCodename = "coffee_beverages_explained";
        var clientName = "test";

        var fixtureContent = await ReadFixtureAsync($"DeliveryClient{Path.DirectorySeparatorChar}{itemCodename}.json");

        var mock = new MockHttpMessageHandler();
        mock.Expect($"{BaseUrl}/items/{itemCodename}")
            .Respond("application/json", fixtureContent);
        mock.Expect($"{BaseUrl}/items/{itemCodename}")
            .Respond("application/json", fixtureContent);

        var mutableOptions = new DeliveryOptions
        {
            EnvironmentId = _guid.ToString(),
            DefaultRenditionPreset = "default"
        };

        var serviceProvider = BuildNamedMemoryCacheServiceProvider(mock, mutableOptions, clientName);
        var client = serviceProvider.GetRequiredKeyedService<IDeliveryClient>(clientName);
        var optionsCache = serviceProvider.GetRequiredService<IOptionsMonitorCache<DeliveryOptions>>();
        var cacheManager = serviceProvider.GetRequiredKeyedService<IDeliveryCacheManager>(clientName);

        var result1 = await client.GetItem<Article>(itemCodename).ExecuteAsync();
        Assert.True(result1.IsSuccess);
        var firstUrl = Assert.Single(result1.Value.Elements.TeaserImage!).Url;

        mutableOptions.DefaultRenditionPreset = null;
        optionsCache.TryRemove(clientName);

        var result2 = await client.GetItem<Article>(itemCodename).ExecuteAsync();
        Assert.True(result2.IsSuccess);
        var secondUrl = Assert.Single(result2.Value.Elements.TeaserImage!).Url;

        var purger = Assert.IsAssignableFrom<IDeliveryCachePurger>(cacheManager);
        await purger.PurgeAsync();

        var result3 = await client.GetItem<Article>(itemCodename).ExecuteAsync();
        Assert.True(result3.IsSuccess);
        var thirdUrl = Assert.Single(result3.Value.Elements.TeaserImage!).Url;

        Assert.False(result1.IsCacheHit);
        Assert.True(result2.IsCacheHit);
        Assert.False(result3.IsCacheHit);
        Assert.Contains("w=200&h=150&fit=clip&rect=7,23,300,200", firstUrl, StringComparison.Ordinal);
        Assert.Equal(firstUrl, secondUrl);
        Assert.DoesNotContain("?", thirdUrl, StringComparison.Ordinal);
        mock.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task MemoryCache_GetItems_CacheHitOnSecondCall()
    {
        var mock = new MockHttpMessageHandler();
        var fixtureContent = await ReadFixtureAsync($"DeliveryClient{Path.DirectorySeparatorChar}articles.json");

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

        // Verify ResponseSource property
        Assert.Equal(ResponseSource.Origin, result1.ResponseSource);
        Assert.Equal(ResponseSource.Cache, result2.ResponseSource);

        // Verify ResponseHeaders property
        Assert.NotNull(result1.ResponseHeaders); // API response has headers
        Assert.Null(result2.ResponseHeaders);    // Cache hit has no headers

        // Verify only one API call was made
        mock.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task MemoryCache_GetItem_DifferentGenericModels_DoNotEvictEachOther()
    {
        var mock = new MockHttpMessageHandler();
        var itemCodename = "coffee_beverages_explained";
        var fixtureContent = await ReadFixtureAsync($"DeliveryClient{Path.DirectorySeparatorChar}{itemCodename}.json");

        mock.Expect($"{BaseUrl}/items/{itemCodename}")
            .Respond("application/json", fixtureContent);
        mock.Expect($"{BaseUrl}/items/{itemCodename}")
            .Respond("application/json", fixtureContent);

        var client = CreateClientWithMemoryCache(mock);

        var articleResult1 = await client.GetItem<Article>(itemCodename).ExecuteAsync();
        var accessoryResult1 = await client.GetItem<Accessory>(itemCodename).ExecuteAsync();
        var articleResult2 = await client.GetItem<Article>(itemCodename).ExecuteAsync();

        Assert.True(articleResult1.IsSuccess);
        Assert.True(accessoryResult1.IsSuccess);
        Assert.True(articleResult2.IsSuccess);
        Assert.False(articleResult1.IsCacheHit);
        Assert.False(accessoryResult1.IsCacheHit);
        Assert.True(articleResult2.IsCacheHit);

        mock.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task DynamicItemQuery_WithCacheConfigured_DoesNotReturnCacheHit()
    {
        var mock = new MockHttpMessageHandler();
        var itemCodename = "coffee_beverages_explained";
        var fixtureContent = await ReadFixtureAsync($"DeliveryClient{Path.DirectorySeparatorChar}{itemCodename}.json");

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
        var fixtureContent = await ReadFixtureAsync($"DeliveryClient{Path.DirectorySeparatorChar}items.json");

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
        var fixtureContent = await ReadFixtureAsync($"DeliveryClient{Path.DirectorySeparatorChar}{itemCodename}.json");

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

        await cacheManager.InvalidateAsync([$"item_{itemCodename}"]);

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
        var fixtureContent = await ReadFixtureAsync($"DeliveryClient{Path.DirectorySeparatorChar}articles.json");

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

        await cacheManager.InvalidateAsync([DeliveryCacheDependencies.ItemsListScope]);

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

        var itemFixture = await ReadFixtureAsync($"DeliveryClient{Path.DirectorySeparatorChar}{itemCodename}.json");

        var itemsFixture = await ReadFixtureAsync($"DeliveryClient{Path.DirectorySeparatorChar}articles.json");

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

        await cacheManager.InvalidateAsync([DeliveryCacheDependencies.ItemsListScope]);

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
        var fixtureContent = await ReadFixtureAsync($"DeliveryClient{Path.DirectorySeparatorChar}article.json");

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

        // Verify ResponseSource property
        Assert.Equal(ResponseSource.Origin, result1.ResponseSource);
        Assert.Equal(ResponseSource.Cache, result2.ResponseSource);

        // Verify ResponseHeaders property
        Assert.NotNull(result1.ResponseHeaders); // API response has headers
        Assert.Null(result2.ResponseHeaders);    // Cache hit has no headers

        // Verify only one API call was made
        mock.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task MemoryCache_GetType_PreservesDependencyKeys_OnCacheHit()
    {
        var mock = new MockHttpMessageHandler();
        var fixtureContent = await ReadFixtureAsync($"DeliveryClient{Path.DirectorySeparatorChar}article.json");

        mock.Expect($"{BaseUrl}/types/article")
            .Respond("application/json", fixtureContent);

        var client = CreateClientWithMemoryCache(mock);

        var result1 = await client.GetType("article").ExecuteAsync();
        var result2 = await client.GetType("article").ExecuteAsync();

        Assert.False(result1.IsCacheHit);
        Assert.True(result2.IsCacheHit);

        Assert.NotNull(result1.DependencyKeys);
        Assert.NotNull(result2.DependencyKeys);
        Assert.Equal(
            result1.DependencyKeys.OrderBy(k => k, StringComparer.Ordinal).ToArray(),
            result2.DependencyKeys.OrderBy(k => k, StringComparer.Ordinal).ToArray());
        Assert.Contains("type_article", result2.DependencyKeys);

        mock.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task MemoryCache_GetTaxonomy_CacheHitOnSecondCall()
    {
        var mock = new MockHttpMessageHandler();
        var fixtureContent = await ReadFixtureAsync($"DeliveryClient{Path.DirectorySeparatorChar}taxonomies_personas.json");

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

        // Verify ResponseSource property
        Assert.Equal(ResponseSource.Origin, result1.ResponseSource);
        Assert.Equal(ResponseSource.Cache, result2.ResponseSource);

        // Verify ResponseHeaders property
        Assert.NotNull(result1.ResponseHeaders); // API response has headers
        Assert.Null(result2.ResponseHeaders);    // Cache hit has no headers

        // Verify only one API call was made
        mock.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task MemoryCache_GetTaxonomy_PreservesDependencyKeys_OnCacheHit()
    {
        var mock = new MockHttpMessageHandler();
        var fixtureContent = await ReadFixtureAsync($"DeliveryClient{Path.DirectorySeparatorChar}taxonomies_personas.json");

        mock.Expect($"{BaseUrl}/taxonomies/personas")
            .Respond("application/json", fixtureContent);

        var client = CreateClientWithMemoryCache(mock);

        var result1 = await client.GetTaxonomy("personas").ExecuteAsync();
        var result2 = await client.GetTaxonomy("personas").ExecuteAsync();

        Assert.False(result1.IsCacheHit);
        Assert.True(result2.IsCacheHit);

        Assert.NotNull(result1.DependencyKeys);
        Assert.NotNull(result2.DependencyKeys);
        Assert.Equal(
            result1.DependencyKeys.OrderBy(k => k, StringComparer.Ordinal).ToArray(),
            result2.DependencyKeys.OrderBy(k => k, StringComparer.Ordinal).ToArray());
        Assert.Contains("taxonomy_personas", result2.DependencyKeys);

        mock.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task MemoryCache_TypesListScopeInvalidation_RefreshesAllCachedTypeLists()
    {
        var mock = new MockHttpMessageHandler();
        var fixtureContent = await ReadFixtureAsync($"DeliveryClient{Path.DirectorySeparatorChar}types_accessory.json");

        mock.Expect($"{BaseUrl}/types*")
            .Respond("application/json", fixtureContent);
        mock.Expect($"{BaseUrl}/types*")
            .Respond("application/json", fixtureContent);
        mock.Expect($"{BaseUrl}/types*")
            .Respond("application/json", fixtureContent);
        mock.Expect($"{BaseUrl}/types*")
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

        var listResultA1 = await client.GetTypes().Skip(1).ExecuteAsync();
        var listResultA2 = await client.GetTypes().Skip(1).ExecuteAsync();
        var listResultB1 = await client.GetTypes().Skip(1).WithElements("title").ExecuteAsync();
        var listResultB2 = await client.GetTypes().Skip(1).WithElements("title").ExecuteAsync();

        Assert.True(listResultA1.IsSuccess);
        Assert.True(listResultA2.IsSuccess);
        Assert.True(listResultB1.IsSuccess);
        Assert.True(listResultB2.IsSuccess);

        Assert.False(listResultA1.IsCacheHit);
        Assert.True(listResultA2.IsCacheHit);
        Assert.False(listResultB1.IsCacheHit);
        Assert.True(listResultB2.IsCacheHit);

        await cacheManager.InvalidateAsync([DeliveryCacheDependencies.TypesListScope]);

        var listResultA3 = await client.GetTypes().Skip(1).ExecuteAsync();
        var listResultB3 = await client.GetTypes().Skip(1).WithElements("title").ExecuteAsync();

        Assert.True(listResultA3.IsSuccess);
        Assert.True(listResultB3.IsSuccess);
        Assert.False(listResultA3.IsCacheHit);
        Assert.False(listResultB3.IsCacheHit);

        mock.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task MemoryCache_GetTypes_PreservesListScopeAndDependencyKeys_OnCacheHit()
    {
        var mock = new MockHttpMessageHandler();
        var fixtureContent = await ReadFixtureAsync($"DeliveryClient{Path.DirectorySeparatorChar}types_accessory.json");

        mock.Expect($"{BaseUrl}/types?skip=1")
            .Respond("application/json", fixtureContent);

        var client = CreateClientWithMemoryCache(mock);

        var result1 = await client.GetTypes().Skip(1).ExecuteAsync();
        var result2 = await client.GetTypes().Skip(1).ExecuteAsync();

        Assert.False(result1.IsCacheHit);
        Assert.True(result2.IsCacheHit);

        Assert.NotNull(result1.DependencyKeys);
        Assert.NotNull(result2.DependencyKeys);
        Assert.Contains(DeliveryCacheDependencies.TypesListScope, result2.DependencyKeys);
        Assert.Contains("type_accessory", result2.DependencyKeys);
        Assert.Contains("type_article", result2.DependencyKeys);
        Assert.Equal(
            result1.DependencyKeys.OrderBy(k => k, StringComparer.Ordinal).ToArray(),
            result2.DependencyKeys.OrderBy(k => k, StringComparer.Ordinal).ToArray());

        mock.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task MemoryCache_TaxonomiesListScopeInvalidation_RefreshesAllCachedTaxonomyLists()
    {
        var mock = new MockHttpMessageHandler();
        var fixtureContent = await ReadFixtureAsync($"DeliveryClient{Path.DirectorySeparatorChar}taxonomies_multiple.json");

        mock.Expect($"{BaseUrl}/taxonomies*")
            .Respond("application/json", fixtureContent);
        mock.Expect($"{BaseUrl}/taxonomies*")
            .Respond("application/json", fixtureContent);
        mock.Expect($"{BaseUrl}/taxonomies*")
            .Respond("application/json", fixtureContent);
        mock.Expect($"{BaseUrl}/taxonomies*")
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

        var listResultA1 = await client.GetTaxonomies().Skip(1).ExecuteAsync();
        var listResultA2 = await client.GetTaxonomies().Skip(1).ExecuteAsync();
        var listResultB1 = await client.GetTaxonomies().Skip(1).Limit(2).ExecuteAsync();
        var listResultB2 = await client.GetTaxonomies().Skip(1).Limit(2).ExecuteAsync();

        Assert.True(listResultA1.IsSuccess);
        Assert.True(listResultA2.IsSuccess);
        Assert.True(listResultB1.IsSuccess);
        Assert.True(listResultB2.IsSuccess);

        Assert.False(listResultA1.IsCacheHit);
        Assert.True(listResultA2.IsCacheHit);
        Assert.False(listResultB1.IsCacheHit);
        Assert.True(listResultB2.IsCacheHit);

        await cacheManager.InvalidateAsync([DeliveryCacheDependencies.TaxonomiesListScope]);

        var listResultA3 = await client.GetTaxonomies().Skip(1).ExecuteAsync();
        var listResultB3 = await client.GetTaxonomies().Skip(1).Limit(2).ExecuteAsync();

        Assert.True(listResultA3.IsSuccess);
        Assert.True(listResultB3.IsSuccess);
        Assert.False(listResultA3.IsCacheHit);
        Assert.False(listResultB3.IsCacheHit);

        mock.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task MemoryCache_GetTaxonomies_PreservesListScopeAndDependencyKeys_OnCacheHit()
    {
        var mock = new MockHttpMessageHandler();
        var fixtureContent = await ReadFixtureAsync($"DeliveryClient{Path.DirectorySeparatorChar}taxonomies_multiple.json");

        mock.Expect($"{BaseUrl}/taxonomies?skip=1")
            .Respond("application/json", fixtureContent);

        var client = CreateClientWithMemoryCache(mock);

        var result1 = await client.GetTaxonomies().Skip(1).ExecuteAsync();
        var result2 = await client.GetTaxonomies().Skip(1).ExecuteAsync();

        Assert.False(result1.IsCacheHit);
        Assert.True(result2.IsCacheHit);

        Assert.NotNull(result1.DependencyKeys);
        Assert.NotNull(result2.DependencyKeys);
        Assert.Contains(DeliveryCacheDependencies.TaxonomiesListScope, result2.DependencyKeys);
        Assert.Contains("taxonomy_personas", result2.DependencyKeys);
        Assert.Contains("taxonomy_processing", result2.DependencyKeys);
        Assert.Equal(
            result1.DependencyKeys.OrderBy(k => k, StringComparer.Ordinal).ToArray(),
            result2.DependencyKeys.OrderBy(k => k, StringComparer.Ordinal).ToArray());

        mock.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task MemoryCache_InvalidatingTypesListScope_DoesNotInvalidateSingleTypeQueries()
    {
        var mock = new MockHttpMessageHandler();
        var typeFixture = await ReadFixtureAsync($"DeliveryClient{Path.DirectorySeparatorChar}article.json");
        var typesFixture = await ReadFixtureAsync($"DeliveryClient{Path.DirectorySeparatorChar}types_accessory.json");

        mock.Expect($"{BaseUrl}/types/article")
            .Respond("application/json", typeFixture);
        mock.Expect($"{BaseUrl}/types?skip=1")
            .Respond("application/json", typesFixture);
        mock.Expect($"{BaseUrl}/types?skip=1")
            .Respond("application/json", typesFixture);

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

        var typeResult1 = await client.GetType("article").ExecuteAsync();
        var typesResult1 = await client.GetTypes().Skip(1).ExecuteAsync();
        var typeResult2 = await client.GetType("article").ExecuteAsync();
        var typesResult2 = await client.GetTypes().Skip(1).ExecuteAsync();

        await cacheManager.InvalidateAsync([DeliveryCacheDependencies.TypesListScope]);

        var typeResult3 = await client.GetType("article").ExecuteAsync();
        var typesResult3 = await client.GetTypes().Skip(1).ExecuteAsync();

        Assert.True(typeResult1.IsSuccess);
        Assert.True(typeResult2.IsSuccess);
        Assert.True(typeResult3.IsSuccess);
        Assert.True(typesResult1.IsSuccess);
        Assert.True(typesResult2.IsSuccess);
        Assert.True(typesResult3.IsSuccess);

        Assert.False(typeResult1.IsCacheHit);
        Assert.True(typeResult2.IsCacheHit);
        Assert.True(typeResult3.IsCacheHit);
        Assert.False(typesResult1.IsCacheHit);
        Assert.True(typesResult2.IsCacheHit);
        Assert.False(typesResult3.IsCacheHit);

        mock.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task MemoryCache_InvalidatingTaxonomiesListScope_DoesNotInvalidateSingleTaxonomyQueries()
    {
        var mock = new MockHttpMessageHandler();
        var taxonomyFixture = await ReadFixtureAsync($"DeliveryClient{Path.DirectorySeparatorChar}taxonomies_personas.json");
        var taxonomiesFixture = await ReadFixtureAsync($"DeliveryClient{Path.DirectorySeparatorChar}taxonomies_multiple.json");

        mock.Expect($"{BaseUrl}/taxonomies/personas")
            .Respond("application/json", taxonomyFixture);
        mock.Expect($"{BaseUrl}/taxonomies?skip=1")
            .Respond("application/json", taxonomiesFixture);
        mock.Expect($"{BaseUrl}/taxonomies?skip=1")
            .Respond("application/json", taxonomiesFixture);

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

        var taxonomyResult1 = await client.GetTaxonomy("personas").ExecuteAsync();
        var taxonomiesResult1 = await client.GetTaxonomies().Skip(1).ExecuteAsync();
        var taxonomyResult2 = await client.GetTaxonomy("personas").ExecuteAsync();
        var taxonomiesResult2 = await client.GetTaxonomies().Skip(1).ExecuteAsync();

        await cacheManager.InvalidateAsync([DeliveryCacheDependencies.TaxonomiesListScope]);

        var taxonomyResult3 = await client.GetTaxonomy("personas").ExecuteAsync();
        var taxonomiesResult3 = await client.GetTaxonomies().Skip(1).ExecuteAsync();

        Assert.True(taxonomyResult1.IsSuccess);
        Assert.True(taxonomyResult2.IsSuccess);
        Assert.True(taxonomyResult3.IsSuccess);
        Assert.True(taxonomiesResult1.IsSuccess);
        Assert.True(taxonomiesResult2.IsSuccess);
        Assert.True(taxonomiesResult3.IsSuccess);

        Assert.False(taxonomyResult1.IsCacheHit);
        Assert.True(taxonomyResult2.IsCacheHit);
        Assert.True(taxonomyResult3.IsCacheHit);
        Assert.False(taxonomiesResult1.IsCacheHit);
        Assert.True(taxonomiesResult2.IsCacheHit);
        Assert.False(taxonomiesResult3.IsCacheHit);

        mock.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task MemoryCache_InvalidatingTypeDependency_RefreshesSingleTypeAndTypeLists()
    {
        var mock = new MockHttpMessageHandler();
        var typeFixture = await ReadFixtureAsync($"DeliveryClient{Path.DirectorySeparatorChar}article.json");
        var typesFixture = await ReadFixtureAsync($"DeliveryClient{Path.DirectorySeparatorChar}types_accessory.json");

        mock.Expect($"{BaseUrl}/types/article")
            .Respond("application/json", typeFixture);
        mock.Expect($"{BaseUrl}/types?skip=1")
            .Respond("application/json", typesFixture);
        mock.Expect($"{BaseUrl}/types/article")
            .Respond("application/json", typeFixture);
        mock.Expect($"{BaseUrl}/types?skip=1")
            .Respond("application/json", typesFixture);

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

        var typeResult1 = await client.GetType("article").ExecuteAsync();
        var typesResult1 = await client.GetTypes().Skip(1).ExecuteAsync();
        var typeResult2 = await client.GetType("article").ExecuteAsync();
        var typesResult2 = await client.GetTypes().Skip(1).ExecuteAsync();

        await cacheManager.InvalidateAsync(["type_article"]);

        var typeResult3 = await client.GetType("article").ExecuteAsync();
        var typesResult3 = await client.GetTypes().Skip(1).ExecuteAsync();

        Assert.True(typeResult1.IsSuccess);
        Assert.True(typeResult2.IsSuccess);
        Assert.True(typeResult3.IsSuccess);
        Assert.True(typesResult1.IsSuccess);
        Assert.True(typesResult2.IsSuccess);
        Assert.True(typesResult3.IsSuccess);

        Assert.False(typeResult1.IsCacheHit);
        Assert.True(typeResult2.IsCacheHit);
        Assert.False(typeResult3.IsCacheHit);
        Assert.False(typesResult1.IsCacheHit);
        Assert.True(typesResult2.IsCacheHit);
        Assert.False(typesResult3.IsCacheHit);

        mock.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task MemoryCache_InvalidatingTaxonomyDependency_RefreshesSingleTaxonomyAndTaxonomyLists()
    {
        var mock = new MockHttpMessageHandler();
        var taxonomyFixture = await ReadFixtureAsync($"DeliveryClient{Path.DirectorySeparatorChar}taxonomies_personas.json");
        var taxonomiesFixture = await ReadFixtureAsync($"DeliveryClient{Path.DirectorySeparatorChar}taxonomies_multiple.json");

        mock.Expect($"{BaseUrl}/taxonomies/personas")
            .Respond("application/json", taxonomyFixture);
        mock.Expect($"{BaseUrl}/taxonomies?skip=1")
            .Respond("application/json", taxonomiesFixture);
        mock.Expect($"{BaseUrl}/taxonomies/personas")
            .Respond("application/json", taxonomyFixture);
        mock.Expect($"{BaseUrl}/taxonomies?skip=1")
            .Respond("application/json", taxonomiesFixture);

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

        var taxonomyResult1 = await client.GetTaxonomy("personas").ExecuteAsync();
        var taxonomiesResult1 = await client.GetTaxonomies().Skip(1).ExecuteAsync();
        var taxonomyResult2 = await client.GetTaxonomy("personas").ExecuteAsync();
        var taxonomiesResult2 = await client.GetTaxonomies().Skip(1).ExecuteAsync();

        await cacheManager.InvalidateAsync(["taxonomy_personas"]);

        var taxonomyResult3 = await client.GetTaxonomy("personas").ExecuteAsync();
        var taxonomiesResult3 = await client.GetTaxonomies().Skip(1).ExecuteAsync();

        Assert.True(taxonomyResult1.IsSuccess);
        Assert.True(taxonomyResult2.IsSuccess);
        Assert.True(taxonomyResult3.IsSuccess);
        Assert.True(taxonomiesResult1.IsSuccess);
        Assert.True(taxonomiesResult2.IsSuccess);
        Assert.True(taxonomiesResult3.IsSuccess);

        Assert.False(taxonomyResult1.IsCacheHit);
        Assert.True(taxonomyResult2.IsCacheHit);
        Assert.False(taxonomyResult3.IsCacheHit);
        Assert.False(taxonomiesResult1.IsCacheHit);
        Assert.True(taxonomiesResult2.IsCacheHit);
        Assert.False(taxonomiesResult3.IsCacheHit);

        mock.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task MemoryCache_GetItem_ConcurrentMisses_AreCoalescedToSingleApiCall()
    {
        var itemCodename = "coffee_beverages_explained";
        var fixtureContent = await ReadFixtureAsync($"DeliveryClient{Path.DirectorySeparatorChar}{itemCodename}.json");

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

    [Fact]
    public async Task MemoryCache_GetItems_ConcurrentMisses_AreCoalescedToSingleApiCall()
    {
        var fixtureContent = await ReadFixtureAsync($"DeliveryClient{Path.DirectorySeparatorChar}articles.json");

        var handler = new DelayedJsonResponseHandler(fixtureContent, TimeSpan.FromMilliseconds(100));
        var client = CreateClientWithMemoryCache(handler);

        var results = await Task.WhenAll(
            Enumerable.Range(0, 12)
                .Select(_ => client.GetItems<Article>()
                    .Where(f => f.System("type").IsEqualTo("article"))
                    .ExecuteAsync()));

        Assert.All(results, result => Assert.True(result.IsSuccess));
        Assert.Equal(1, handler.RequestCount);
        Assert.Single(results, r => !r.IsCacheHit);
    }

    [Fact]
    public async Task MemoryCache_GetTypes_ConcurrentMisses_AreCoalescedToSingleApiCall()
    {
        var fixtureContent = await ReadFixtureAsync($"DeliveryClient{Path.DirectorySeparatorChar}types_accessory.json");

        var handler = new DelayedJsonResponseHandler(fixtureContent, TimeSpan.FromMilliseconds(100));
        var client = CreateClientWithMemoryCache(handler);

        var results = await Task.WhenAll(
            Enumerable.Range(0, 12)
                .Select(_ => client.GetTypes().Skip(1).ExecuteAsync()));

        Assert.All(results, result => Assert.True(result.IsSuccess));
        Assert.Equal(1, handler.RequestCount);
        Assert.Single(results, r => !r.IsCacheHit);
    }

    [Fact]
    public async Task MemoryCache_GetTaxonomies_ConcurrentMisses_AreCoalescedToSingleApiCall()
    {
        var fixtureContent = await ReadFixtureAsync($"DeliveryClient{Path.DirectorySeparatorChar}taxonomies_multiple.json");

        var handler = new DelayedJsonResponseHandler(fixtureContent, TimeSpan.FromMilliseconds(100));
        var client = CreateClientWithMemoryCache(handler);

        var results = await Task.WhenAll(
            Enumerable.Range(0, 12)
                .Select(_ => client.GetTaxonomies().Skip(1).ExecuteAsync()));

        Assert.All(results, result => Assert.True(result.IsSuccess));
        Assert.Equal(1, handler.RequestCount);
        Assert.Single(results, r => !r.IsCacheHit);
    }

    [Fact]
    public async Task MemoryCache_ConcurrentItemsMiss_WithScopeInvalidationRace_CompletesAndSupportsRefresh()
    {
        var fixtureContent = await ReadFixtureAsync($"DeliveryClient{Path.DirectorySeparatorChar}articles.json");
        var handler = new DelayedJsonResponseHandler(fixtureContent, TimeSpan.FromMilliseconds(100));
        var options = new DeliveryOptions
        {
            EnvironmentId = _guid.ToString()
        };

        var serviceProvider = BuildNamedMemoryCacheServiceProvider(handler, options, defaultExpiration: TimeSpan.FromMinutes(5));
        var client = serviceProvider.GetRequiredKeyedService<IDeliveryClient>("test");
        var cacheManager = serviceProvider.GetRequiredKeyedService<IDeliveryCacheManager>("test");

        var queryTask = Task.WhenAll(
            Enumerable.Range(0, 12)
                .Select(_ => client.GetItems<Article>()
                    .Where(f => f.System("type").IsEqualTo("article"))
                    .ExecuteAsync()));

        var invalidateTask = Task.Run(async () =>
        {
            await handler.WaitForFirstRequestAsync();
            await cacheManager.InvalidateAsync([DeliveryCacheDependencies.ItemsListScope]);
        });

        var allWork = Task.WhenAll(queryTask, invalidateTask);
        await allWork.WaitAsync(TimeSpan.FromSeconds(5));

        var queryResults = await queryTask;
        Assert.All(queryResults, result => Assert.True(result.IsSuccess));

        // Deterministic final invalidation to verify refresh path remains healthy after the race.
        await cacheManager.InvalidateAsync([DeliveryCacheDependencies.ItemsListScope]);

        var refreshed = await client.GetItems<Article>()
            .Where(f => f.System("type").IsEqualTo("article"))
            .ExecuteAsync();

        Assert.True(refreshed.IsSuccess);
        Assert.False(refreshed.IsCacheHit);
    }

    #endregion

    #region Hybrid Cache Integration Tests

    [Fact]
    public async Task HybridCache_GetItem_CacheHitOnSecondCall()
    {
        var mock = new MockHttpMessageHandler();
        var itemCodename = "coffee_beverages_explained";
        var fixtureContent = await ReadFixtureAsync($"DeliveryClient{Path.DirectorySeparatorChar}{itemCodename}.json");

        // Raw JSON caching is now reliable - use Expect() to verify only one API call
        mock.Expect($"{BaseUrl}/items/{itemCodename}")
            .Respond("application/json", fixtureContent);

        var client = CreateClientWithHybridCache(mock);

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

        // Verify ResponseSource property
        Assert.Equal(ResponseSource.Origin, result1.ResponseSource);
        Assert.Equal(ResponseSource.Cache, result2.ResponseSource);

        // Verify only one API call was made
        mock.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task HybridCache_GetItems_CacheHitOnSecondCall()
    {
        var mock = new MockHttpMessageHandler();
        var fixtureContent = await ReadFixtureAsync($"DeliveryClient{Path.DirectorySeparatorChar}articles.json");

        // Raw JSON caching is now reliable - use Expect() to verify only one API call
        mock.Expect($"{BaseUrl}/items?system.type%5Beq%5D=article")
            .Respond("application/json", fixtureContent);

        var client = CreateClientWithHybridCache(mock);

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

        // Verify ResponseSource property
        Assert.Equal(ResponseSource.Origin, result1.ResponseSource);
        Assert.Equal(ResponseSource.Cache, result2.ResponseSource);

        // Verify only one API call was made
        mock.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task HybridCache_CorruptedModularContentPayload_FallsBackToApiAndRecaches()
    {
        var mock = new MockHttpMessageHandler();
        var itemCodename = "coffee_beverages_explained";
        var fixtureContent = await ReadFixtureAsync($"DeliveryClient{Path.DirectorySeparatorChar}{itemCodename}.json");

        mock.Expect($"{BaseUrl}/items/{itemCodename}")
            .Respond("application/json", fixtureContent);

        var options = new DeliveryOptions
        {
            EnvironmentId = _guid.ToString()
        };

        var mockDistributedCache = new MockDistributedCache();
        SeedCorruptedDistributedItemPayload(mockDistributedCache, "test", itemCodename, fixtureContent);
        var serviceProvider = BuildNamedHybridCacheServiceProvider(mock, options, mockDistributedCache);
        var client = serviceProvider.GetRequiredKeyedService<IDeliveryClient>("test");

        var result1 = await client.GetItem<Article>(itemCodename).ExecuteAsync();
        var result2 = await client.GetItem<Article>(itemCodename).ExecuteAsync();

        Assert.True(result1.IsSuccess);
        Assert.True(result2.IsSuccess);
        Assert.False(result1.IsCacheHit);
        Assert.True(result2.IsCacheHit);
        mock.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task HybridCache_Invalidation_RefreshesCache()
    {
        var mock = new MockHttpMessageHandler();
        var itemCodename = "coffee_beverages_explained";
        var fixtureContent = await ReadFixtureAsync($"DeliveryClient{Path.DirectorySeparatorChar}{itemCodename}.json");

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
        services.AddDeliveryHybridCache("test");

        var serviceProvider = services.BuildServiceProvider();
        var client = serviceProvider.GetRequiredKeyedService<IDeliveryClient>("test");
        var cacheManager = serviceProvider.GetRequiredKeyedService<IDeliveryCacheManager>("test");

        var result1 = await client.GetItem<Article>(itemCodename).ExecuteAsync();

        await cacheManager.InvalidateAsync([$"item_{itemCodename}"]);

        var result2 = await client.GetItem<Article>(itemCodename).ExecuteAsync();

        Assert.True(result1.IsSuccess);
        Assert.True(result2.IsSuccess);
        Assert.NotNull(result1.Value);
        Assert.NotNull(result2.Value);
    }

    [Fact]
    public async Task HybridCache_ItemsListScopeInvalidation_RefreshesAllCachedItemLists()
    {
        var mock = new MockHttpMessageHandler();
        var fixtureContent = await ReadFixtureAsync($"DeliveryClient{Path.DirectorySeparatorChar}articles.json");

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
        services.AddDeliveryHybridCache("test");

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

        await cacheManager.InvalidateAsync([DeliveryCacheDependencies.ItemsListScope]);

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
    public async Task HybridCache_TypesListScopeInvalidation_RefreshesAllCachedTypeLists()
    {
        var mock = new MockHttpMessageHandler();
        var fixtureContent = await ReadFixtureAsync($"DeliveryClient{Path.DirectorySeparatorChar}types_accessory.json");

        mock.When($"{BaseUrl}/types*")
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
        services.AddDeliveryHybridCache("test");

        var serviceProvider = services.BuildServiceProvider();
        var client = serviceProvider.GetRequiredKeyedService<IDeliveryClient>("test");
        var cacheManager = serviceProvider.GetRequiredKeyedService<IDeliveryCacheManager>("test");

        var listResultA1 = await client.GetTypes().Skip(1).ExecuteAsync();
        var listResultA2 = await client.GetTypes().Skip(1).ExecuteAsync();
        var listResultB1 = await client.GetTypes().Skip(1).WithElements("title").ExecuteAsync();
        var listResultB2 = await client.GetTypes().Skip(1).WithElements("title").ExecuteAsync();

        Assert.True(listResultA1.IsSuccess);
        Assert.True(listResultA2.IsSuccess);
        Assert.True(listResultB1.IsSuccess);
        Assert.True(listResultB2.IsSuccess);

        Assert.False(listResultA1.IsCacheHit);
        Assert.False(listResultB1.IsCacheHit);

        await cacheManager.InvalidateAsync([DeliveryCacheDependencies.TypesListScope]);

        var listResultA3 = await client.GetTypes().Skip(1).ExecuteAsync();
        var listResultB3 = await client.GetTypes().Skip(1).WithElements("title").ExecuteAsync();

        Assert.True(listResultA3.IsSuccess);
        Assert.True(listResultB3.IsSuccess);
        Assert.False(listResultA3.IsCacheHit);
        Assert.False(listResultB3.IsCacheHit);
    }

    [Fact]
    public async Task HybridCache_InvalidatingTypeDependency_RefreshesSingleTypeAndTypeLists()
    {
        var mock = new MockHttpMessageHandler();
        var typeFixture = await ReadFixtureAsync($"DeliveryClient{Path.DirectorySeparatorChar}article.json");
        var typesFixture = await ReadFixtureAsync($"DeliveryClient{Path.DirectorySeparatorChar}types_accessory.json");

        mock.Expect($"{BaseUrl}/types/article")
            .Respond("application/json", typeFixture);
        mock.Expect($"{BaseUrl}/types?skip=1")
            .Respond("application/json", typesFixture);
        mock.Expect($"{BaseUrl}/types/article")
            .Respond("application/json", typeFixture);
        mock.Expect($"{BaseUrl}/types?skip=1")
            .Respond("application/json", typesFixture);

        var services = new ServiceCollection();
        var options = new DeliveryOptions
        {
            EnvironmentId = _guid.ToString()
        };

        var mockDistributedCache = new MockDistributedCache();
        services.AddSingleton<IDistributedCache>(mockDistributedCache);
        services.AddDeliveryClient("test", o => DeliveryOptionsCopyHelper.Copy(options, o),
            configureHttpClient: b => b.ConfigurePrimaryHttpMessageHandler(() => mock));
        services.AddDeliveryHybridCache("test");

        var serviceProvider = services.BuildServiceProvider();
        var client = serviceProvider.GetRequiredKeyedService<IDeliveryClient>("test");
        var cacheManager = serviceProvider.GetRequiredKeyedService<IDeliveryCacheManager>("test");

        var typeResult1 = await client.GetType("article").ExecuteAsync();
        var typesResult1 = await client.GetTypes().Skip(1).ExecuteAsync();
        var typeResult2 = await client.GetType("article").ExecuteAsync();
        var typesResult2 = await client.GetTypes().Skip(1).ExecuteAsync();

        await cacheManager.InvalidateAsync(["type_article"]);

        var typeResult3 = await client.GetType("article").ExecuteAsync();
        var typesResult3 = await client.GetTypes().Skip(1).ExecuteAsync();

        Assert.True(typeResult1.IsSuccess);
        Assert.True(typeResult2.IsSuccess);
        Assert.True(typeResult3.IsSuccess);
        Assert.True(typesResult1.IsSuccess);
        Assert.True(typesResult2.IsSuccess);
        Assert.True(typesResult3.IsSuccess);

        Assert.False(typeResult1.IsCacheHit);
        Assert.True(typeResult2.IsCacheHit);
        Assert.False(typeResult3.IsCacheHit);
        Assert.False(typesResult1.IsCacheHit);
        Assert.True(typesResult2.IsCacheHit);
        Assert.False(typesResult3.IsCacheHit);

        mock.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task HybridCache_TaxonomiesListScopeInvalidation_RefreshesAllCachedTaxonomyLists()
    {
        var mock = new MockHttpMessageHandler();
        var fixtureContent = await ReadFixtureAsync($"DeliveryClient{Path.DirectorySeparatorChar}taxonomies_multiple.json");

        mock.Expect($"{BaseUrl}/taxonomies*")
            .Respond("application/json", fixtureContent);
        mock.Expect($"{BaseUrl}/taxonomies*")
            .Respond("application/json", fixtureContent);
        mock.Expect($"{BaseUrl}/taxonomies*")
            .Respond("application/json", fixtureContent);
        mock.Expect($"{BaseUrl}/taxonomies*")
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
        services.AddDeliveryHybridCache("test");

        var serviceProvider = services.BuildServiceProvider();
        var client = serviceProvider.GetRequiredKeyedService<IDeliveryClient>("test");
        var cacheManager = serviceProvider.GetRequiredKeyedService<IDeliveryCacheManager>("test");

        var listResultA1 = await client.GetTaxonomies().Skip(1).ExecuteAsync();
        var listResultA2 = await client.GetTaxonomies().Skip(1).ExecuteAsync();
        var listResultB1 = await client.GetTaxonomies().Skip(1).Limit(2).ExecuteAsync();
        var listResultB2 = await client.GetTaxonomies().Skip(1).Limit(2).ExecuteAsync();

        Assert.True(listResultA1.IsSuccess);
        Assert.True(listResultA2.IsSuccess);
        Assert.True(listResultB1.IsSuccess);
        Assert.True(listResultB2.IsSuccess);

        Assert.False(listResultA1.IsCacheHit);
        Assert.True(listResultA2.IsCacheHit);
        Assert.False(listResultB1.IsCacheHit);
        Assert.True(listResultB2.IsCacheHit);

        await cacheManager.InvalidateAsync([DeliveryCacheDependencies.TaxonomiesListScope]);

        var listResultA3 = await client.GetTaxonomies().Skip(1).ExecuteAsync();
        var listResultB3 = await client.GetTaxonomies().Skip(1).Limit(2).ExecuteAsync();

        Assert.True(listResultA3.IsSuccess);
        Assert.True(listResultB3.IsSuccess);
        Assert.False(listResultA3.IsCacheHit);
        Assert.False(listResultB3.IsCacheHit);

        mock.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task HybridCache_GetItem_ConcurrentMisses_AreCoalescedToSingleApiCall()
    {
        var itemCodename = "coffee_beverages_explained";
        var fixtureContent = await ReadFixtureAsync($"DeliveryClient{Path.DirectorySeparatorChar}{itemCodename}.json");

        var handler = new DelayedJsonResponseHandler(fixtureContent, TimeSpan.FromMilliseconds(100));
        var client = CreateClientWithHybridCache(handler);

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

    [Fact]
    public async Task HybridCache_GetItems_ConcurrentMisses_AreCoalescedToSingleApiCall()
    {
        var fixtureContent = await ReadFixtureAsync($"DeliveryClient{Path.DirectorySeparatorChar}articles.json");

        var handler = new DelayedJsonResponseHandler(fixtureContent, TimeSpan.FromMilliseconds(100));
        var client = CreateClientWithHybridCache(handler);

        var results = await Task.WhenAll(
            Enumerable.Range(0, 12)
                .Select(_ => client.GetItems<Article>()
                    .Where(f => f.System("type").IsEqualTo("article"))
                    .ExecuteAsync()));

        Assert.All(results, result => Assert.True(result.IsSuccess));
        Assert.Equal(1, handler.RequestCount);
        Assert.Single(results, r => !r.IsCacheHit);
    }

    [Fact]
    public async Task HybridCache_ConcurrentTypesMiss_WithScopeInvalidationRace_CompletesAndSupportsRefresh()
    {
        var fixtureContent = await ReadFixtureAsync($"DeliveryClient{Path.DirectorySeparatorChar}types_accessory.json");
        var handler = new DelayedJsonResponseHandler(fixtureContent, TimeSpan.FromMilliseconds(100));
        var options = new DeliveryOptions
        {
            EnvironmentId = _guid.ToString()
        };

        var serviceProvider = BuildNamedHybridCacheServiceProvider(handler, options, new MockDistributedCache(), defaultExpiration: TimeSpan.FromMinutes(5));
        var client = serviceProvider.GetRequiredKeyedService<IDeliveryClient>("test");
        var cacheManager = serviceProvider.GetRequiredKeyedService<IDeliveryCacheManager>("test");

        var queryTask = Task.WhenAll(
            Enumerable.Range(0, 12)
                .Select(_ => client.GetTypes().Skip(1).ExecuteAsync()));

        var invalidateTask = Task.Run(async () =>
        {
            await handler.WaitForFirstRequestAsync();
            await cacheManager.InvalidateAsync([DeliveryCacheDependencies.TypesListScope]);
        });

        var allWork = Task.WhenAll(queryTask, invalidateTask);
        await allWork.WaitAsync(TimeSpan.FromSeconds(5));

        var queryResults = await queryTask;
        Assert.All(queryResults, result => Assert.True(result.IsSuccess));

        // Deterministic final invalidation to verify refresh path remains healthy after the race.
        await cacheManager.InvalidateAsync([DeliveryCacheDependencies.TypesListScope]);

        var refreshed = await client.GetTypes().Skip(1).ExecuteAsync();
        Assert.True(refreshed.IsSuccess);
        Assert.False(refreshed.IsCacheHit);
    }

    [Fact]
    public async Task MemoryCache_FailSafe_SetsResponseSourceToFailSafe()
    {
        var mock = new MockHttpMessageHandler();
        var itemCodename = "coffee_beverages_explained";
        var fixtureContent = await ReadFixtureAsync($"DeliveryClient{Path.DirectorySeparatorChar}{itemCodename}.json");

        var options = new DeliveryOptions
        {
            EnvironmentId = _guid.ToString()
        };

        var services = new ServiceCollection();
        AddNamedDeliveryClient(services, "test", options, mock);
        services.AddDeliveryMemoryCache("test", opts =>
        {
            opts.DefaultExpiration = TimeSpan.FromMilliseconds(50);
            opts.IsFailSafeEnabled = true;
            opts.FailSafeMaxDuration = TimeSpan.FromMinutes(5);
            opts.FailSafeThrottleDuration = TimeSpan.FromSeconds(1);
        });
        var serviceProvider = services.BuildServiceProvider();
        var client = serviceProvider.GetRequiredKeyedService<IDeliveryClient>("test");

        // First call populates the cache
        mock.Expect($"{BaseUrl}/items/{itemCodename}")
            .Respond("application/json", fixtureContent);

        var result1 = await client.GetItem<Article>(itemCodename).ExecuteAsync();
        Assert.True(result1.IsSuccess);
        Assert.Equal(ResponseSource.Origin, result1.ResponseSource);

        // Wait for the cache entry to expire
        await Task.Delay(200);

        // Second call: API returns 500, fail-safe should serve stale data
        mock.Expect($"{BaseUrl}/items/{itemCodename}")
            .Respond(HttpStatusCode.InternalServerError, "application/json", """{"message":"Server error","error_code":500}""");

        var result2 = await client.GetItem<Article>(itemCodename).ExecuteAsync();

        Assert.True(result2.IsSuccess);
        Assert.True(result2.IsCacheHit);
        Assert.Equal(ResponseSource.FailSafe, result2.ResponseSource);
        Assert.NotNull(result2.Value);
        Assert.Equal(result1.Value.Elements.Title, result2.Value.Elements.Title);

        mock.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task HybridCache_FailSafe_SetsResponseSourceToFailSafe()
    {
        var mock = new MockHttpMessageHandler();
        var itemCodename = "coffee_beverages_explained";
        var fixtureContent = await ReadFixtureAsync($"DeliveryClient{Path.DirectorySeparatorChar}{itemCodename}.json");

        var options = new DeliveryOptions
        {
            EnvironmentId = _guid.ToString()
        };

        var mockDistributedCache = new MockDistributedCache();
        var services = new ServiceCollection();
        services.AddSingleton<IDistributedCache>(mockDistributedCache);
        AddNamedDeliveryClient(services, "test", options, mock);
        services.AddDeliveryHybridCache("test", opts =>
        {
            opts.DefaultExpiration = TimeSpan.FromMilliseconds(50);
            opts.IsFailSafeEnabled = true;
            opts.FailSafeMaxDuration = TimeSpan.FromMinutes(5);
            opts.FailSafeThrottleDuration = TimeSpan.FromSeconds(1);
        });
        var serviceProvider = services.BuildServiceProvider();
        var client = serviceProvider.GetRequiredKeyedService<IDeliveryClient>("test");

        // First call populates the cache
        mock.Expect($"{BaseUrl}/items/{itemCodename}")
            .Respond("application/json", fixtureContent);

        var result1 = await client.GetItem<Article>(itemCodename).ExecuteAsync();
        Assert.True(result1.IsSuccess);
        Assert.Equal(ResponseSource.Origin, result1.ResponseSource);

        // Wait for the cache entry to expire
        await Task.Delay(200);

        // Second call: API returns 500, fail-safe should serve stale data
        mock.Expect($"{BaseUrl}/items/{itemCodename}")
            .Respond(HttpStatusCode.InternalServerError, "application/json", """{"message":"Server error","error_code":500}""");

        var result2 = await client.GetItem<Article>(itemCodename).ExecuteAsync();

        Assert.True(result2.IsSuccess);
        Assert.True(result2.IsCacheHit);
        Assert.Equal(ResponseSource.FailSafe, result2.ResponseSource);
        Assert.NotNull(result2.Value);
        Assert.Equal(result1.Value.Elements.Title, result2.Value.Elements.Title);

        mock.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task MemoryCache_FailSafe_ConcurrentRequests_ReportFailSafeForAllResponses()
    {
        var itemCodename = "coffee_beverages_explained";
        var fixtureContent = await ReadFixtureAsync($"DeliveryClient{Path.DirectorySeparatorChar}{itemCodename}.json");
        var handler = new PrimedSuccessThenErrorHandler(
            fixtureContent,
            """{"message":"Server error","error_code":500}""");

        var options = new DeliveryOptions
        {
            EnvironmentId = _guid.ToString()
        };

        var services = new ServiceCollection();
        AddNamedDeliveryClient(services, "test", options, handler);
        services.AddDeliveryMemoryCache("test", opts =>
        {
            opts.DefaultExpiration = TimeSpan.FromMilliseconds(50);
            opts.IsFailSafeEnabled = true;
            opts.FailSafeMaxDuration = TimeSpan.FromMinutes(5);
            opts.FailSafeThrottleDuration = TimeSpan.FromSeconds(1);
        });
        var serviceProvider = services.BuildServiceProvider();
        var client = serviceProvider.GetRequiredKeyedService<IDeliveryClient>("test");

        var result1 = await client.GetItem<Article>(itemCodename).ExecuteAsync();
        Assert.True(result1.IsSuccess);
        Assert.Equal(ResponseSource.Origin, result1.ResponseSource);

        await Task.Delay(200);

        var failSafeSeed = await client.GetItem<Article>(itemCodename).ExecuteAsync();
        Assert.True(failSafeSeed.IsSuccess);
        Assert.True(failSafeSeed.IsCacheHit);
        Assert.Equal(ResponseSource.FailSafe, failSafeSeed.ResponseSource);
        var requestCountAfterSeed = handler.RequestCount;
        Assert.True(requestCountAfterSeed >= 2);

        var concurrentResults = await Task.WhenAll(
            Enumerable.Range(0, 12).Select(_ => client.GetItem<Article>(itemCodename).ExecuteAsync()));

        Assert.All(concurrentResults, result =>
        {
            Assert.True(result.IsSuccess);
            Assert.True(result.IsCacheHit);
            Assert.Equal(ResponseSource.FailSafe, result.ResponseSource);
        });
        Assert.Equal(requestCountAfterSeed, handler.RequestCount);
    }

    [Fact]
    public async Task HybridCache_FailSafe_ConcurrentRequests_ReportFailSafeForAllResponses()
    {
        var itemCodename = "coffee_beverages_explained";
        var fixtureContent = await ReadFixtureAsync($"DeliveryClient{Path.DirectorySeparatorChar}{itemCodename}.json");
        var handler = new PrimedSuccessThenErrorHandler(
            fixtureContent,
            """{"message":"Server error","error_code":500}""");

        var options = new DeliveryOptions
        {
            EnvironmentId = _guid.ToString(),
            EnableResilience = false
        };

        var services = new ServiceCollection();
        services.AddSingleton<IDistributedCache>(new MockDistributedCache());
        AddNamedDeliveryClient(services, "test", options, handler);
        services.AddDeliveryHybridCache("test", opts =>
        {
            opts.DefaultExpiration = TimeSpan.FromMilliseconds(50);
            opts.IsFailSafeEnabled = true;
            opts.FailSafeMaxDuration = TimeSpan.FromMinutes(5);
            opts.FailSafeThrottleDuration = TimeSpan.FromSeconds(1);
        });
        var serviceProvider = services.BuildServiceProvider();
        var client = serviceProvider.GetRequiredKeyedService<IDeliveryClient>("test");

        var result1 = await client.GetItem<Article>(itemCodename).ExecuteAsync();
        Assert.True(result1.IsSuccess);
        Assert.Equal(ResponseSource.Origin, result1.ResponseSource);

        await Task.Delay(200);

        var failSafeSeed = await client.GetItem<Article>(itemCodename).ExecuteAsync();
        Assert.True(failSafeSeed.IsSuccess);
        Assert.True(failSafeSeed.IsCacheHit);
        Assert.Equal(ResponseSource.FailSafe, failSafeSeed.ResponseSource);
        var requestCountAfterSeed = handler.RequestCount;
        Assert.True(requestCountAfterSeed >= 2);

        var concurrentResults = await Task.WhenAll(
            Enumerable.Range(0, 12).Select(_ => client.GetItem<Article>(itemCodename).ExecuteAsync()));

        Assert.All(concurrentResults, result =>
        {
            Assert.True(result.IsSuccess);
            Assert.True(result.IsCacheHit);
            Assert.Equal(ResponseSource.FailSafe, result.ResponseSource);
        });
        Assert.Equal(requestCountAfterSeed, handler.RequestCount);
    }

    [Fact]
    public async Task MemoryCache_FailSafe_ThrottleWindow_KeepsResponseSourceFailSafe()
    {
        var itemCodename = "coffee_beverages_explained";
        var fixtureContent = await ReadFixtureAsync($"DeliveryClient{Path.DirectorySeparatorChar}{itemCodename}.json");
        var handler = new PrimedSuccessThenErrorHandler(
            fixtureContent,
            """{"message":"Server error","error_code":500}""");

        var options = new DeliveryOptions
        {
            EnvironmentId = _guid.ToString(),
            EnableResilience = false
        };

        var services = new ServiceCollection();
        AddNamedDeliveryClient(services, "test", options, handler);
        services.AddDeliveryMemoryCache("test", opts =>
        {
            opts.DefaultExpiration = TimeSpan.FromMilliseconds(50);
            opts.IsFailSafeEnabled = true;
            opts.FailSafeMaxDuration = TimeSpan.FromMinutes(5);
            opts.FailSafeThrottleDuration = TimeSpan.FromSeconds(5);
        });
        var serviceProvider = services.BuildServiceProvider();
        var client = serviceProvider.GetRequiredKeyedService<IDeliveryClient>("test");

        var result1 = await client.GetItem<Article>(itemCodename).ExecuteAsync();
        Assert.True(result1.IsSuccess);
        Assert.Equal(ResponseSource.Origin, result1.ResponseSource);

        await Task.Delay(200);

        var failSafe1 = await client.GetItem<Article>(itemCodename).ExecuteAsync();
        Assert.True(failSafe1.IsSuccess);
        Assert.True(failSafe1.IsCacheHit);
        Assert.Equal(ResponseSource.FailSafe, failSafe1.ResponseSource);
        var requestCountAfterFirstFailSafe = handler.RequestCount;
        Assert.True(requestCountAfterFirstFailSafe >= 2);

        var failSafe2 = await client.GetItem<Article>(itemCodename).ExecuteAsync();
        Assert.True(failSafe2.IsSuccess);
        Assert.True(failSafe2.IsCacheHit);
        Assert.Equal(ResponseSource.FailSafe, failSafe2.ResponseSource);
        Assert.Equal(requestCountAfterFirstFailSafe, handler.RequestCount);
    }

    #endregion

    #region Dependency Tracking Integration Tests

    [Fact]
    public async Task MemoryCache_ItemWithModularContent_TracksAllDependencies()
    {
        var mock = new MockHttpMessageHandler();
        var itemCodename = "coffee_processing_techniques";
        var fixtureContent = await ReadFixtureAsync($"ContentLinkResolver{Path.DirectorySeparatorChar}{itemCodename}.json");

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
        var fixtureContent = await ReadFixtureAsync($"ContentLinkResolver{Path.DirectorySeparatorChar}{itemCodename}.json");

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
        var fixtureContent = await ReadFixtureAsync($"DeliveryClient{Path.DirectorySeparatorChar}articles.json");

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
    public async Task MemoryCache_GetType_TracksTypeDependency()
    {
        var mock = new MockHttpMessageHandler();
        var fixtureContent = await ReadFixtureAsync($"DeliveryClient{Path.DirectorySeparatorChar}article.json");

        mock.When($"{BaseUrl}/types/article")
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

        var result = await client.GetType("article").ExecuteAsync();

        Assert.True(result.IsSuccess);

        var cachedEntry = Assert.Single(mockCacheManager.CachedItems);
        Assert.Contains("type_article", cachedEntry.Dependencies);
    }

    [Fact]
    public async Task MemoryCache_GetTypes_TracksTypesListScopeAndTypeDependencies()
    {
        var mock = new MockHttpMessageHandler();
        var fixtureContent = await ReadFixtureAsync($"DeliveryClient{Path.DirectorySeparatorChar}types_accessory.json");

        mock.When($"{BaseUrl}/types?skip=1")
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

        var result = await client.GetTypes().Skip(1).ExecuteAsync();

        Assert.True(result.IsSuccess);

        var cachedEntry = Assert.Single(mockCacheManager.CachedItems);
        Assert.Contains(DeliveryCacheDependencies.TypesListScope, cachedEntry.Dependencies);
        Assert.Contains(cachedEntry.Dependencies, dependency => dependency.StartsWith("type_", StringComparison.Ordinal));
    }

    [Fact]
    public async Task MemoryCache_GetTaxonomy_TracksTaxonomyDependency()
    {
        var mock = new MockHttpMessageHandler();
        var fixtureContent = await ReadFixtureAsync($"DeliveryClient{Path.DirectorySeparatorChar}taxonomies_personas.json");

        mock.When($"{BaseUrl}/taxonomies/personas")
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

        var result = await client.GetTaxonomy("personas").ExecuteAsync();

        Assert.True(result.IsSuccess);

        var cachedEntry = Assert.Single(mockCacheManager.CachedItems);
        Assert.Contains("taxonomy_personas", cachedEntry.Dependencies);
    }

    [Fact]
    public async Task MemoryCache_GetTaxonomies_TracksTaxonomiesListScopeAndTaxonomyDependencies()
    {
        var mock = new MockHttpMessageHandler();
        var fixtureContent = await ReadFixtureAsync($"DeliveryClient{Path.DirectorySeparatorChar}taxonomies_multiple.json");

        mock.When($"{BaseUrl}/taxonomies?skip=1")
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

        var result = await client.GetTaxonomies().Skip(1).ExecuteAsync();

        Assert.True(result.IsSuccess);

        var cachedEntry = Assert.Single(mockCacheManager.CachedItems);
        Assert.Contains(DeliveryCacheDependencies.TaxonomiesListScope, cachedEntry.Dependencies);
        Assert.Contains(cachedEntry.Dependencies, dependency => dependency.StartsWith("taxonomy_", StringComparison.Ordinal));
    }

    [Fact]
    public async Task UnkeyedCacheManagerRegistration_IsIgnoredByClientCachingPath()
    {
        var mock = new MockHttpMessageHandler();
        var itemCodename = "coffee_beverages_explained";

        var itemFixture = await ReadFixtureAsync($"DeliveryClient{Path.DirectorySeparatorChar}{itemCodename}.json");

        var itemsFixture = await ReadFixtureAsync($"DeliveryClient{Path.DirectorySeparatorChar}articles.json");

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
            new MemoryCacheManager(sp.GetRequiredService<IMemoryCache>(), new DeliveryCacheOptions()));
        services.AddDeliveryClient(options, configureHttpClient: b =>
            b.ConfigurePrimaryHttpMessageHandler(() => mock));

        var serviceProvider = services.BuildServiceProvider();
        var client = serviceProvider.GetRequiredService<IDeliveryClient>();
        var cacheManager = serviceProvider.GetRequiredService<IDeliveryCacheManager>();

        Assert.NotNull(cacheManager); // Registered unkeyed manager is still available in DI.

        var singleResult1 = await client.GetItem<Article>(itemCodename).ExecuteAsync();
        var singleResult2 = await client.GetItem<Article>(itemCodename).ExecuteAsync();
        var listResult1 = await client.GetItems<Article>()
            .Where(f => f.System("type").IsEqualTo("article"))
            .ExecuteAsync();
        var listResult2 = await client.GetItems<Article>()
            .Where(f => f.System("type").IsEqualTo("article"))
            .ExecuteAsync();

        Assert.True(singleResult1.IsSuccess);
        Assert.True(singleResult2.IsSuccess);
        Assert.True(listResult1.IsSuccess);
        Assert.True(listResult2.IsSuccess);
        Assert.False(singleResult1.IsCacheHit);
        Assert.False(singleResult2.IsCacheHit);
        Assert.False(listResult1.IsCacheHit);
        Assert.False(listResult2.IsCacheHit);
    }

    #endregion

    #region Cache Disabled Tests

    [Fact]
    public async Task CachingDisabled_AlwaysHitsApi()
    {
        var mock = new MockHttpMessageHandler();
        var itemCodename = "coffee_beverages_explained";
        var fixtureContent = await ReadFixtureAsync($"DeliveryClient{Path.DirectorySeparatorChar}{itemCodename}.json");

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
        var fixtureContent = await ReadFixtureAsync($"DeliveryClient{Path.DirectorySeparatorChar}{itemCodename}.json");

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

    private static Task<string> ReadFixtureAsync(string fixtureRelativePath) =>
        File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory, "Fixtures", fixtureRelativePath));

    private IDeliveryClient CreateClientWithMemoryCache(HttpMessageHandler httpHandler, DeliveryOptions? options = null)
    {
        options ??= new DeliveryOptions
        {
            EnvironmentId = _guid.ToString()
        };

        var provider = BuildNamedMemoryCacheServiceProvider(httpHandler, options);
        return provider.GetRequiredKeyedService<IDeliveryClient>("test");
    }

    private IDeliveryClient CreateClientWithHybridCache(HttpMessageHandler httpHandler)
    {
        var options = new DeliveryOptions
        {
            EnvironmentId = _guid.ToString()
        };

        var provider = BuildNamedHybridCacheServiceProvider(httpHandler, options, new MockDistributedCache());
        return provider.GetRequiredKeyedService<IDeliveryClient>("test");
    }

    private static void AddNamedDeliveryClient(
        IServiceCollection services,
        string clientName,
        DeliveryOptions options,
        HttpMessageHandler httpHandler)
    {
        services.AddDeliveryClient(clientName, o => DeliveryOptionsCopyHelper.Copy(options, o),
            configureHttpClient: b => b.ConfigurePrimaryHttpMessageHandler(() => httpHandler));
    }

    private static ServiceProvider BuildNamedMemoryCacheServiceProvider(
        HttpMessageHandler httpHandler,
        DeliveryOptions options,
        string clientName = "test",
        TimeSpan? defaultExpiration = null)
    {
        var services = new ServiceCollection();
        AddNamedDeliveryClient(services, clientName, options, httpHandler);
        services.AddDeliveryMemoryCache(clientName, defaultExpiration: defaultExpiration);
        return services.BuildServiceProvider();
    }

    private static ServiceProvider BuildNamedHybridCacheServiceProvider(
        HttpMessageHandler httpHandler,
        DeliveryOptions options,
        IDistributedCache distributedCache,
        string clientName = "test",
        TimeSpan? defaultExpiration = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton(distributedCache);
        AddNamedDeliveryClient(services, clientName, options, httpHandler);
        services.AddDeliveryHybridCache(clientName, defaultExpiration: defaultExpiration);
        return services.BuildServiceProvider();
    }

    private static void SeedCorruptedDistributedItemPayload(
        MockDistributedCache distributedCache,
        string clientName,
        string itemCodename,
        string singleItemResponseJson)
    {
        using var responseDoc = JsonDocument.Parse(singleItemResponseJson);
        var itemJson = responseDoc.RootElement.GetProperty("item").GetRawText();

        var payload = new CachedRawItemsPayload
        {
            ItemsJson = [itemJson],
            ModularContentJson = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["broken-linked-item"] = "{ this is not valid json }"
            }
        };

        var sdkCacheKey = CacheKeyBuilder.BuildItemKey(itemCodename, new SingleItemParams(), modelType: null);
        var distributedCacheKey = $"{clientName}:cache:{sdkCacheKey}";
        var serializedPayload = JsonSerializer.Serialize(payload);

        distributedCache.Set(
            distributedCacheKey,
            Encoding.UTF8.GetBytes(serializedPayload),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) });
    }

    #endregion
}
