using System.Net;
using System.Text.Json.Serialization;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.Extensions;
using Microsoft.Extensions.DependencyInjection;
using RichardSzalay.MockHttp;
using Xunit;

namespace Kontent.Ai.Delivery.Tests.QueryBuilders;

public sealed class ExecuteAllAsyncTests
{
    [Fact]
    public async Task ExecuteAllAsync_WithLimit1_FetchesAllPages_ByIncrementingSkip_UntilEmptyPage()
    {
        var env = Guid.NewGuid().ToString();
        var baseUrl = $"https://deliver.kontent.ai/{env}";
        var itemsUrl = $"{baseUrl}/items";
        var mockHttp = new MockHttpMessageHandler();

        mockHttp.Expect(itemsUrl).WithQueryString("skip", "0").WithQueryString("limit", "1")
            .Respond("application/json", BuildItemsListingJson(skip: 0, limit: 1, codenames: ["a0"]));

        mockHttp.Expect(itemsUrl).WithQueryString("skip", "1").WithQueryString("limit", "1")
            .Respond("application/json", BuildItemsListingJson(skip: 1, limit: 1, codenames: ["a1"]));

        mockHttp.Expect(itemsUrl).WithQueryString("skip", "2").WithQueryString("limit", "1")
            .Respond("application/json", BuildItemsListingJson(skip: 2, limit: 1, codenames: ["a2"]));

        // Terminating page: no items => ExecuteAllAsync should stop
        mockHttp.Expect(itemsUrl).WithQueryString("skip", "3").WithQueryString("limit", "1")
            .Respond("application/json", BuildItemsListingJson(skip: 3, limit: 1, codenames: []));

        var services = new ServiceCollection();
        services.AddDeliveryClient(
            new DeliveryOptions { EnvironmentId = env },
            configureHttpClient: b => b.ConfigurePrimaryHttpMessageHandler(() => mockHttp));

        var client = services.BuildServiceProvider().GetRequiredService<IDeliveryClient>();

        var result = await client.GetItems<IDynamicElements>()
            .Limit(1)
            .ExecuteAllAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Equal(3, result.Value.Items.Count);
        Assert.Equal(["a0", "a1", "a2"], result.Value.Items.Select(i => i.System.Codename).ToArray());

        mockHttp.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task ExecuteAllAsync_WithLimit1_FetchesAllPages_ForDynamicQueryBuilder()
    {
        var env = Guid.NewGuid().ToString();
        var baseUrl = $"https://deliver.kontent.ai/{env}";
        var itemsUrl = $"{baseUrl}/items";
        var mockHttp = new MockHttpMessageHandler();

        mockHttp.Expect(itemsUrl).WithQueryString("skip", "0").WithQueryString("limit", "1")
            .Respond("application/json", BuildItemsListingJson(skip: 0, limit: 1, codenames: ["a0"]));

        mockHttp.Expect(itemsUrl).WithQueryString("skip", "1").WithQueryString("limit", "1")
            .Respond("application/json", BuildItemsListingJson(skip: 1, limit: 1, codenames: ["a1"]));

        mockHttp.Expect(itemsUrl).WithQueryString("skip", "2").WithQueryString("limit", "1")
            .Respond("application/json", BuildItemsListingJson(skip: 2, limit: 1, codenames: ["a2"]));

        // Terminating page: no items => ExecuteAllAsync should stop
        mockHttp.Expect(itemsUrl).WithQueryString("skip", "3").WithQueryString("limit", "1")
            .Respond("application/json", BuildItemsListingJson(skip: 3, limit: 1, codenames: []));

        var services = new ServiceCollection();
        services.AddDeliveryClient(
            new DeliveryOptions { EnvironmentId = env },
            configureHttpClient: b => b.ConfigurePrimaryHttpMessageHandler(() => mockHttp));

        var client = services.BuildServiceProvider().GetRequiredService<IDeliveryClient>();

        // NOTE: This uses the non-generic dynamic query builder (DynamicItemsQuery).
        var result = await client.GetItems()
            .Limit(1)
            .ExecuteAllAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Equal(3, result.Value.Items.Count);
        Assert.Equal(["a0", "a1", "a2"], result.Value.Items.Select(i => i.System.Codename).ToArray());

        mockHttp.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task ExecuteAllAsync_WithLimit1_FetchesAllPages_ForStronglyTypedItemsQuery()
    {
        var env = Guid.NewGuid().ToString();
        var baseUrl = $"https://deliver.kontent.ai/{env}";
        var itemsUrl = $"{baseUrl}/items";
        var mockHttp = new MockHttpMessageHandler();

        mockHttp.Expect(itemsUrl).WithQueryString("skip", "0").WithQueryString("limit", "1")
            .Respond("application/json", BuildItemsListingJson(skip: 0, limit: 1, codenames: ["a0"], elementsJsonFactory: BuildTitleOnlyElementsJson));

        mockHttp.Expect(itemsUrl).WithQueryString("skip", "1").WithQueryString("limit", "1")
            .Respond("application/json", BuildItemsListingJson(skip: 1, limit: 1, codenames: ["a1"], elementsJsonFactory: BuildTitleOnlyElementsJson));

        mockHttp.Expect(itemsUrl).WithQueryString("skip", "2").WithQueryString("limit", "1")
            .Respond("application/json", BuildItemsListingJson(skip: 2, limit: 1, codenames: ["a2"], elementsJsonFactory: BuildTitleOnlyElementsJson));

        // Terminating page: no items => ExecuteAllAsync should stop
        mockHttp.Expect(itemsUrl).WithQueryString("skip", "3").WithQueryString("limit", "1")
            .Respond("application/json", BuildItemsListingJson(skip: 3, limit: 1, codenames: [], elementsJsonFactory: BuildTitleOnlyElementsJson));

        var services = new ServiceCollection();
        services.AddDeliveryClient(
            new DeliveryOptions { EnvironmentId = env },
            configureHttpClient: b => b.ConfigurePrimaryHttpMessageHandler(() => mockHttp));

        var client = services.BuildServiceProvider().GetRequiredService<IDeliveryClient>();

        var result = await client.GetItems<TitleOnly>()
            .Limit(1)
            .ExecuteAllAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Equal(3, result.Value.Items.Count);
        Assert.Equal(["a0", "a1", "a2"], result.Value.Items.Select(i => i.System.Codename).ToArray());

        // Sanity-check that element deserialization worked (not strictly required for the pagination test,
        // but ensures the typed path is exercised).
        Assert.All(result.Value.Items, i => Assert.Equal("Title 0", i.Elements.Title));

        mockHttp.VerifyNoOutstandingExpectation();
    }

    private static string BuildItemsListingJson(
        int skip,
        int limit,
        IReadOnlyList<string> codenames,
        Func<int, string>? elementsJsonFactory = null)
    {
        elementsJsonFactory ??= _ => string.Empty;

        var itemsJson = string.Join(",", codenames.Select((codename, idx) => $$"""
            {
              "system": {
                "id": "{{Guid.NewGuid()}}",
                "name": "Item {{idx}}",
                "codename": "{{codename}}",
                "language": "default",
                "type": "article",
                "collection": "default",
                "last_modified": "2024-01-01T00:00:00Z"
              },
              "elements": { {{elementsJsonFactory(idx)}} }
            }
            """));

        return $$"""
        {
          "items": [{{itemsJson}}],
          "pagination": { "skip": {{skip}}, "limit": {{limit}}, "count": {{codenames.Count}}, "next_page": "" },
          "modular_content": {}
        }
        """;
    }

    private static string BuildTitleOnlyElementsJson(int idx) => $$"""
      "title": {
        "type": "text",
        "name": "Title",
        "value": "Title {{idx}}"
      }
      """;

    private sealed record TitleOnly
    {
        [JsonPropertyName("title")]
        public string? Title { get; init; }
    }
}

