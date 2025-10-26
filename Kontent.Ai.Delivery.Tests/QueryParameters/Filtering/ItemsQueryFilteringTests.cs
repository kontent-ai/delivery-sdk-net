using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.Extensions;
using Microsoft.Extensions.DependencyInjection;
using RichardSzalay.MockHttp;
using Xunit;

namespace Kontent.Ai.Delivery.Tests.QueryParameters.Filtering;

public class ItemsQueryFilteringTests
{
    [Fact]
    public async Task ItemsQuery_Where_ComposesFilterParameter()
    {
        var env = Guid.NewGuid().ToString();
        var baseUrl = $"https://deliver.kontent.ai/{env}";
        var itemsUrl = $"{baseUrl}/items";
        var mockHttp = new MockHttpMessageHandler();

        mockHttp.When($"{itemsUrl}?system.type%5Beq%5D=article")
            .Respond("application/json", await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}items.json")));

        var services = new ServiceCollection();
        services.AddDeliveryClient(new DeliveryOptions { EnvironmentId = env }, configureHttpClient: b => b.ConfigurePrimaryHttpMessageHandler(() => mockHttp));

        var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<IDeliveryClient>();

        var result = await client.GetItems<IElementsModel>()
            .Where(new Api.QueryBuilders.Filtering.ItemFilters().Equals(ItemSystemPath.Type, "article"))
            .ExecuteAsync();

        Assert.True(result.IsSuccess);
    }
}