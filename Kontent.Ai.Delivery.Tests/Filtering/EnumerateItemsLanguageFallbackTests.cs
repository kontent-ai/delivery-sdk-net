using Kontent.Ai.Delivery.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using RichardSzalay.MockHttp;

namespace Kontent.Ai.Delivery.Tests.Filtering;

public class EnumerateItemsLanguageFallbackTests
{
    [Fact]
    public async Task ItemsFeed_WithLanguageFallbackDisabled_AddsSystemLanguageFilter()
    {
        var env = Guid.NewGuid().ToString();
        var baseUrl = $"https://deliver.kontent.ai/{env}";
        var feedUrl = $"{baseUrl}/items-feed";
        var mockHttp = new MockHttpMessageHandler();

        mockHttp.Expect(feedUrl)
            .WithQueryString("language", "es-ES")
            .WithQueryString("system.language[eq]", "es-ES")
            .Respond("application/json",
                await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory,
                    $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}articles_feed.json")));

        var services = new ServiceCollection();
        services.AddDeliveryClient(new DeliveryOptions { EnvironmentId = env },
            configureHttpClient: b => b.ConfigurePrimaryHttpMessageHandler(() => mockHttp));

        var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<IDeliveryClient>();

        var items = new List<IContentItem>();
        await foreach (var item in client.GetItemsFeed()
            .WithLanguage("es-ES", LanguageFallbackMode.Disabled)
            .EnumerateItemsAsync())
        {
            items.Add(item);
        }

        Assert.NotEmpty(items);
        mockHttp.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task DynamicEnumerateItemsQuery_ChainedParameters_AreForwarded()
    {
        var env = Guid.NewGuid().ToString();
        var baseUrl = $"https://deliver.kontent.ai/{env}";
        var feedUrl = $"{baseUrl}/items-feed";
        var mockHttp = new MockHttpMessageHandler();

        mockHttp.Expect(feedUrl)
            .WithQueryString("language", "es-ES")
            .WithQueryString("elements", "title")
            .WithQueryString("order", "system.name[desc]")
            .WithQueryString("elements.title[eq]", "Coffee")
            .With(req => !req.Headers.Contains("X-KC-Wait-For-Loading-New-Content"))
            .Respond("application/json",
                await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory,
                    $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}articles_feed.json")));

        var services = new ServiceCollection();
        services.AddDeliveryClient(new DeliveryOptions { EnvironmentId = env },
            configureHttpClient: b => b.ConfigurePrimaryHttpMessageHandler(() => mockHttp));

        var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<IDeliveryClient>();

        var result = await client.GetItemsFeed()
            .WithLanguage("es-ES")
            .WithElements("title")
            .OrderBy("system.name", OrderingMode.Descending)
            .WaitForLoadingNewContent(false)
            .Where(f => f.Element("title").IsEqualTo("Coffee"))
            .ExecuteAsync();

        Assert.True(result.IsSuccess);
        mockHttp.VerifyNoOutstandingExpectation();
    }
}
