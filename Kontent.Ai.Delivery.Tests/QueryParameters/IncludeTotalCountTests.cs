using System;
using System.Threading.Tasks;
using FluentAssertions;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.Extensions;
using Kontent.Ai.Delivery.SharedModels;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RichardSzalay.MockHttp;
using Xunit;

namespace Kontent.Ai.Delivery.Tests.QueryParameters;

public class IncludeTotalCountTests
{
    private readonly Guid _environmentId = Guid.NewGuid();
    private string BaseUrl => $"https://deliver.kontent.ai/{_environmentId}";
    private readonly MockHttpMessageHandler _mockHttp = new MockHttpMessageHandler();
    private DeliveryOptions Options => new DeliveryOptions
    {
        EnvironmentId = _environmentId.ToString(),
        IncludeTotalCount = true
    };

    private IDeliveryClient CreateClient()
    {
        var services = new ServiceCollection();
        services.AddDeliveryClient(Options, configureHttpClient: b => b.ConfigurePrimaryHttpMessageHandler(() => _mockHttp));
        return services.BuildServiceProvider().GetRequiredService<IDeliveryClient>();
    }

    [Fact]
    public void PaginationResponse_WithoutTotalCount_DeserializedCorrectly()
    {
        var responsePagination = JObject.FromObject(new
        {
            skip = 20,
            limit = 10,
            count = 8,
            next_page = "nextPage"
        });
        var expected = new Pagination()
        {
            Skip = 20,
            Limit = 10,
            Count = 8,
            TotalCount = null,
            NextPageUrl = "nextPage"
        };

        var result = responsePagination.ToObject<Pagination>();

        result.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void PaginationResponse_WithTotalCount_DeserializedCorrectly()
    {
        var responsePagination = JObject.FromObject(new
        {
            skip = 20,
            limit = 10,
            count = 8,
            total_count = 28,
            next_page = "nextPage"
        });
        var expected = new Pagination()
        {
            Skip = 20,
            Limit = 10,
            Count = 8,
            TotalCount = 28,
            NextPageUrl = "nextPage"
        };

        var result = responsePagination.ToObject<Pagination>();

        result.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task GetItems_DeliveryOptionsWithIncludeTotalCount_IncludeTotalCountParameterAdded()
    {
        var responseJson = JsonConvert.SerializeObject(CreateItemsResponse());
        _mockHttp
            .Expect($"{BaseUrl}/items")
            .WithExactQueryString("includeTotalCount")
            .Respond("application/json", responseJson);
        var client = CreateClient();
        await client.GetItems<IElementsModel>().ExecuteAsync();

        _mockHttp.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task GetItemsTyped_DeliveryOptionsWithIncludeTotalCount_IncludeTotalCountParameterAdded()
    {
        var responseJson = JsonConvert.SerializeObject(CreateItemsResponse());
        _mockHttp
            .Expect($"{BaseUrl}/items")
            .WithExactQueryString("system.type=cafe&includeTotalCount")
            .Respond("application/json", responseJson);
        var client = CreateClient();
        await client.GetItems<IElementsModel>()
            .Where(new Api.QueryBuilders.Filtering.Filter(
                "system.type",
                FilterOperator.Equals,
                Api.QueryBuilders.Filtering.StringValue.From("cafe")))
            .ExecuteAsync();

        _mockHttp.VerifyNoOutstandingExpectation();
    }

    private static object CreateItemsResponse() => new
    {
        items = new object[0],
        modular_content = new { },
        pagination = new
        {
            skip = 0,
            limit = 0,
            count = 1,
            total_count = 1,
            next_page = ""
        }
    };
}