using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.Tests.Models.ContentTypes;
using Microsoft.Extensions.DependencyInjection;
using RichardSzalay.MockHttp;

namespace Kontent.Ai.Delivery.Tests.Caching;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class RedisFactAttribute : FactAttribute
{
    public RedisFactAttribute()
    {
        var isEnabled = Environment.GetEnvironmentVariable("KONTENT_SDK_RUN_REDIS_TESTS");
        if (!string.Equals(isEnabled, "true", StringComparison.OrdinalIgnoreCase))
        {
            Skip = "Redis integration tests are disabled. Set KONTENT_SDK_RUN_REDIS_TESTS=true to enable.";
        }
    }
}

public class RedisCacheIntegrationTests
{
    private const string ClientName = "redis";
    private readonly Guid _guid = Guid.NewGuid();
    private string BaseUrl => $"https://deliver.kontent.ai/{_guid}";

    [RedisFact]
    public async Task Redis_ItemAndItemsListInvalidation_IsVisibleAcrossServiceProviders()
    {
        var itemCodename = "coffee_beverages_explained";
        var itemFixture = await ReadFixtureAsync($"DeliveryClient{Path.DirectorySeparatorChar}{itemCodename}.json");
        var listFixture = await ReadFixtureAsync($"DeliveryClient{Path.DirectorySeparatorChar}articles.json");
        var keyPrefix = $"redis-items-{Guid.NewGuid():N}";

        var mockA = CreateItemAndListMock(itemCodename, itemFixture, listFixture);
        var mockB = CreateItemAndListMock(itemCodename, itemFixture, listFixture);

        using var providerA = BuildRedisServiceProvider(mockA, keyPrefix);
        using var providerB = BuildRedisServiceProvider(mockB, keyPrefix);

        var clientA = providerA.GetRequiredKeyedService<IDeliveryClient>(ClientName);
        var clientB = providerB.GetRequiredKeyedService<IDeliveryClient>(ClientName);
        var cacheManagerB = providerB.GetRequiredKeyedService<IDeliveryCacheManager>(ClientName);

        var itemA1 = await clientA.GetItem<Article>(itemCodename).ExecuteAsync();
        var listA1 = await clientA.GetItems<Article>()
            .Where(f => f.System("type").IsEqualTo("article"))
            .ExecuteAsync();

        Assert.True(itemA1.IsSuccess);
        Assert.True(listA1.IsSuccess);
        Assert.False(itemA1.IsCacheHit);
        Assert.False(listA1.IsCacheHit);

        var itemB1 = await clientB.GetItem<Article>(itemCodename).ExecuteAsync();
        var listB1 = await clientB.GetItems<Article>()
            .Where(f => f.System("type").IsEqualTo("article"))
            .ExecuteAsync();

        Assert.True(itemB1.IsSuccess);
        Assert.True(listB1.IsSuccess);
        Assert.True(itemB1.IsCacheHit);
        Assert.True(listB1.IsCacheHit);

        await cacheManagerB.InvalidateAsync(default, $"item_{itemCodename}", DeliveryCacheDependencies.ItemsListScope);

        var itemA2 = await clientA.GetItem<Article>(itemCodename).ExecuteAsync();
        var listA2 = await clientA.GetItems<Article>()
            .Where(f => f.System("type").IsEqualTo("article"))
            .ExecuteAsync();

        Assert.True(itemA2.IsSuccess);
        Assert.True(listA2.IsSuccess);
        Assert.False(itemA2.IsCacheHit);
        Assert.False(listA2.IsCacheHit);
    }

    [RedisFact]
    public async Task Redis_TypeAndTypesListScopeInvalidation_RefreshesDetailAndList()
    {
        var typeFixture = await ReadFixtureAsync($"DeliveryClient{Path.DirectorySeparatorChar}article.json");
        var typesFixture = await ReadFixtureAsync($"DeliveryClient{Path.DirectorySeparatorChar}types_accessory.json");
        var keyPrefix = $"redis-types-{Guid.NewGuid():N}";

        var mock = new MockHttpMessageHandler();
        mock.When($"{BaseUrl}/types/article")
            .Respond("application/json", typeFixture);
        mock.When($"{BaseUrl}/types*")
            .Respond("application/json", typesFixture);

        using var provider = BuildRedisServiceProvider(mock, keyPrefix);
        var client = provider.GetRequiredKeyedService<IDeliveryClient>(ClientName);
        var cacheManager = provider.GetRequiredKeyedService<IDeliveryCacheManager>(ClientName);

        var type1 = await client.GetType("article").ExecuteAsync();
        var list1 = await client.GetTypes().Skip(1).ExecuteAsync();
        var type2 = await client.GetType("article").ExecuteAsync();
        var list2 = await client.GetTypes().Skip(1).ExecuteAsync();

        Assert.True(type1.IsSuccess);
        Assert.True(list1.IsSuccess);
        Assert.True(type2.IsSuccess);
        Assert.True(list2.IsSuccess);
        Assert.False(type1.IsCacheHit);
        Assert.False(list1.IsCacheHit);
        Assert.True(type2.IsCacheHit);
        Assert.True(list2.IsCacheHit);

        await cacheManager.InvalidateAsync(default, "type_article", DeliveryCacheDependencies.TypesListScope);

        var type3 = await client.GetType("article").ExecuteAsync();
        var list3 = await client.GetTypes().Skip(1).ExecuteAsync();

        Assert.True(type3.IsSuccess);
        Assert.True(list3.IsSuccess);
        Assert.False(type3.IsCacheHit);
        Assert.False(list3.IsCacheHit);
    }

    [RedisFact]
    public async Task Redis_TaxonomyAndTaxonomiesListScopeInvalidation_RefreshesDetailAndList()
    {
        var taxonomyFixture = await ReadFixtureAsync($"DeliveryClient{Path.DirectorySeparatorChar}taxonomies_personas.json");
        var taxonomiesFixture = await ReadFixtureAsync($"DeliveryClient{Path.DirectorySeparatorChar}taxonomies_multiple.json");
        var keyPrefix = $"redis-taxonomies-{Guid.NewGuid():N}";

        var mock = new MockHttpMessageHandler();
        mock.When($"{BaseUrl}/taxonomies/personas")
            .Respond("application/json", taxonomyFixture);
        mock.When($"{BaseUrl}/taxonomies*")
            .Respond("application/json", taxonomiesFixture);

        using var provider = BuildRedisServiceProvider(mock, keyPrefix);
        var client = provider.GetRequiredKeyedService<IDeliveryClient>(ClientName);
        var cacheManager = provider.GetRequiredKeyedService<IDeliveryCacheManager>(ClientName);

        var taxonomy1 = await client.GetTaxonomy("personas").ExecuteAsync();
        var list1 = await client.GetTaxonomies().Skip(1).ExecuteAsync();
        var taxonomy2 = await client.GetTaxonomy("personas").ExecuteAsync();
        var list2 = await client.GetTaxonomies().Skip(1).ExecuteAsync();

        Assert.True(taxonomy1.IsSuccess);
        Assert.True(list1.IsSuccess);
        Assert.True(taxonomy2.IsSuccess);
        Assert.True(list2.IsSuccess);
        Assert.False(taxonomy1.IsCacheHit);
        Assert.False(list1.IsCacheHit);
        Assert.True(taxonomy2.IsCacheHit);
        Assert.True(list2.IsCacheHit);

        await cacheManager.InvalidateAsync(default, "taxonomy_personas", DeliveryCacheDependencies.TaxonomiesListScope);

        var taxonomy3 = await client.GetTaxonomy("personas").ExecuteAsync();
        var list3 = await client.GetTaxonomies().Skip(1).ExecuteAsync();

        Assert.True(taxonomy3.IsSuccess);
        Assert.True(list3.IsSuccess);
        Assert.False(taxonomy3.IsCacheHit);
        Assert.False(list3.IsCacheHit);
    }

    private ServiceProvider BuildRedisServiceProvider(MockHttpMessageHandler mockHttp, string keyPrefix)
    {
        var services = new ServiceCollection();

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = GetRedisConnectionString();
            options.InstanceName = string.Empty;
        });

        services.AddDeliveryClient(ClientName, options =>
        {
            options.EnvironmentId = _guid.ToString();
        }, configureHttpClient: builder => builder.ConfigurePrimaryHttpMessageHandler(() => mockHttp));

        services.AddDeliveryHybridCache(ClientName, keyPrefix: keyPrefix, defaultExpiration: TimeSpan.FromMinutes(5));
        return services.BuildServiceProvider();
    }

    private MockHttpMessageHandler CreateItemAndListMock(string itemCodename, string itemFixture, string listFixture)
    {
        var mock = new MockHttpMessageHandler();
        mock.When($"{BaseUrl}/items/{itemCodename}")
            .Respond("application/json", itemFixture);
        mock.When($"{BaseUrl}/items*")
            .Respond("application/json", listFixture);
        return mock;
    }

    private static Task<string> ReadFixtureAsync(string fixtureRelativePath) =>
        File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory, "Fixtures", fixtureRelativePath));

    private static string GetRedisConnectionString()
        => Environment.GetEnvironmentVariable("KONTENT_SDK_REDIS_CONNECTION") ?? "localhost:6379";
}
