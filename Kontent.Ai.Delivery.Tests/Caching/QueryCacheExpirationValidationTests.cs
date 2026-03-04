using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.Tests.Models.ContentTypes;
using Microsoft.Extensions.DependencyInjection;
using RichardSzalay.MockHttp;

namespace Kontent.Ai.Delivery.Tests.Caching;

public sealed class QueryCacheExpirationValidationTests : IDisposable
{
    private const string ClientName = "test";
    private readonly ServiceProvider _serviceProvider;
    private readonly IDeliveryClient _client;

    public QueryCacheExpirationValidationTests()
    {
        var services = new ServiceCollection();
        var mockHttp = new MockHttpMessageHandler();

        services.AddDeliveryClient(ClientName, options =>
        {
            options.EnvironmentId = Guid.NewGuid().ToString();
        }, configureHttpClient: builder => builder.ConfigurePrimaryHttpMessageHandler(() => mockHttp));

        services.AddDeliveryMemoryCache(ClientName);

        _serviceProvider = services.BuildServiceProvider();
        _client = _serviceProvider.GetRequiredKeyedService<IDeliveryClient>(ClientName);
    }

    [Fact]
    public void WithCacheExpiration_PositiveExpiration_DoesNotThrow_ForAllCacheableQueries()
    {
        foreach (var scenario in GetCacheableQueryScenarios())
        {
            var exception = Record.Exception(() => scenario.Apply(TimeSpan.FromMinutes(1)));
            Assert.Null(exception);
        }
    }

    [Fact]
    public void WithCacheExpiration_NullExpiration_DoesNotThrow_ForAllCacheableQueries()
    {
        foreach (var scenario in GetCacheableQueryScenarios())
        {
            var exception = Record.Exception(() => scenario.Apply(null));
            Assert.Null(exception);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void WithCacheExpiration_NonPositiveExpiration_ThrowsActionableException_ForAllCacheableQueries(int expirationSeconds)
    {
        var expiration = TimeSpan.FromSeconds(expirationSeconds);
        var expectedValue = expiration.ToString("c");

        foreach (var scenario in GetCacheableQueryScenarios())
        {
            var exception = Assert.Throws<ArgumentOutOfRangeException>(() => scenario.Apply(expiration));

            Assert.Equal("expiration", exception.ParamName);
            Assert.Contains(scenario.QueryKind, exception.Message, StringComparison.Ordinal);
            Assert.Contains(expectedValue, exception.Message, StringComparison.Ordinal);
            Assert.Contains("Use null to use the cache manager default.", exception.Message, StringComparison.Ordinal);
        }
    }

    public void Dispose() => _serviceProvider.Dispose();

    private IEnumerable<CacheableQueryScenario> GetCacheableQueryScenarios()
    {
        yield return new CacheableQueryScenario(
            "GetItem<T>()",
            expiration => _client.GetItem<Article>("coffee_beverages_explained").WithCacheExpiration(expiration));

        yield return new CacheableQueryScenario(
            "GetItems<T>()",
            expiration => _client.GetItems<Article>().WithCacheExpiration(expiration));

        yield return new CacheableQueryScenario(
            "GetType()",
            expiration => _client.GetType("article").WithCacheExpiration(expiration));

        yield return new CacheableQueryScenario(
            "GetTypes()",
            expiration => _client.GetTypes().WithCacheExpiration(expiration));

        yield return new CacheableQueryScenario(
            "GetTaxonomy()",
            expiration => _client.GetTaxonomy("personas").WithCacheExpiration(expiration));

        yield return new CacheableQueryScenario(
            "GetTaxonomies()",
            expiration => _client.GetTaxonomies().WithCacheExpiration(expiration));
    }

    private readonly record struct CacheableQueryScenario(string QueryKind, Func<TimeSpan?, object> Apply);
}
