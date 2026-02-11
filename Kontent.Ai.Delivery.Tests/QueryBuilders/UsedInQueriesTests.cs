using System.Net;
using System.Text;
using Kontent.Ai.Delivery.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using RichardSzalay.MockHttp;
using Xunit;

namespace Kontent.Ai.Delivery.Tests.QueryBuilders;

public sealed class UsedInQueriesTests
{
    [Fact]
    public async Task UsedInQueries_Continuation_PaginatesAcrossMultiplePages()
    {
        var env = Guid.NewGuid().ToString();
        var usedInUrl = $"https://deliver.kontent.ai/{env}/items/coffee_beverages_explained/used-in";
        var mockHttp = new MockHttpMessageHandler();

        mockHttp.Expect(usedInUrl)
            .Respond(_ => CreateUsedInResponse(["parent_1", "parent_2"], continuationToken: "token_1"));

        mockHttp.Expect(usedInUrl)
            .WithHeaders("X-Continuation", "token_1")
            .Respond(_ => CreateUsedInResponse(["parent_3"], continuationToken: null));

        var client = BuildClient(env, mockHttp);
        var items = new List<IUsedInItem>();

        await foreach (var item in client.GetItemUsedIn("coffee_beverages_explained").EnumerateItemsAsync())
            items.Add(item);

        Assert.Equal(["parent_1", "parent_2", "parent_3"], items.Select(x => x.System.Codename).ToArray());
        mockHttp.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task UsedInQueries_FailedIntermediatePage_StopsEnumerationWithoutThrow()
    {
        var env = Guid.NewGuid().ToString();
        var usedInUrl = $"https://deliver.kontent.ai/{env}/assets/asset_codename/used-in";
        var mockHttp = new MockHttpMessageHandler();

        mockHttp.Expect(usedInUrl)
            .Respond(_ => CreateUsedInResponse(["parent_1"], continuationToken: "token_1"));

        mockHttp.Expect(usedInUrl)
            .WithHeaders("X-Continuation", "token_1")
            .Respond(HttpStatusCode.ServiceUnavailable);

        var client = BuildClient(env, mockHttp);
        var items = new List<IUsedInItem>();

        await foreach (var item in client.GetAssetUsedIn("asset_codename").EnumerateItemsAsync())
            items.Add(item);

        Assert.Single(items);
        Assert.Equal("parent_1", items[0].System.Codename);
        mockHttp.VerifyNoOutstandingExpectation();
    }

    private static IDeliveryClient BuildClient(string env, MockHttpMessageHandler mockHttp)
    {
        var services = new ServiceCollection();
        services.AddDeliveryClient(
            new DeliveryOptions { EnvironmentId = env },
            configureHttpClient: b => b.ConfigurePrimaryHttpMessageHandler(() => mockHttp));

        return services.BuildServiceProvider().GetRequiredService<IDeliveryClient>();
    }

    private static HttpResponseMessage CreateUsedInResponse(IReadOnlyList<string> codenames, string? continuationToken)
    {
        var itemsJson = string.Join(",", codenames.Select(codename =>
            $"{{\"system\":{{\"id\":\"{Guid.NewGuid()}\",\"name\":\"{codename}\",\"codename\":\"{codename}\",\"type\":\"article\",\"last_modified\":\"2024-01-01T00:00:00Z\",\"language\":\"en-US\",\"collection\":\"default\",\"workflow\":\"default\",\"workflow_step\":\"published\"}}}}"));

        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent($"{{\"items\":[{itemsJson}]}}", Encoding.UTF8, "application/json")
        };

        if (!string.IsNullOrEmpty(continuationToken))
            response.Headers.Add("X-Continuation", continuationToken);

        return response;
    }
}
