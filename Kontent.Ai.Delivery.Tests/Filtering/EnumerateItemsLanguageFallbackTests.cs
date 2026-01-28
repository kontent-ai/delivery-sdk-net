using Kontent.Ai.Delivery.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using RichardSzalay.MockHttp;
using Xunit;

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
}

