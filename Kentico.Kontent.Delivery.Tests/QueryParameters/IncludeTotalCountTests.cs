using System;
using FakeItEasy;
using FluentAssertions;
using Kentico.Kontent.Delivery.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RichardSzalay.MockHttp;
using Xunit;

namespace Kentico.Kontent.Delivery.Tests.QueryParameters
{
    public class IncludeTotalCountTests
    {
        private readonly Guid ProjectId = Guid.NewGuid();
        private  string BaseUrl => $"https://deliver.kontent.ai/{ProjectId}";
        private readonly MockHttpMessageHandler MockHttp = new MockHttpMessageHandler();
        private DeliveryOptions Options => new DeliveryOptions
        {
            ProjectId = ProjectId.ToString(),
            EnableRetryPolicy = false,
            IncludeTotalCount = true
        };

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
            var expected = new Pagination(20, 10, 8, null, "nextPage");

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
            var expected = new Pagination(20, 10, 8, 28, "nextPage");

            var result = responsePagination.ToObject<Pagination>();

            result.Should().BeEquivalentTo(expected);
        }

        [Fact]
        public async void GetItems_DeliveryOptionsWithIncludeTotalCount_IncludeTotalCountParameterAdded()
        {
            var responseJson = JsonConvert.SerializeObject(CreateItemsResponse());
            MockHttp
                .Expect($"{BaseUrl}/items")
                .WithExactQueryString("includeTotalCount")
                .Respond("application/json", responseJson);
            var client = Factories.DeliveryClientFactory.GetMockedDeliveryClientWithOptions(Options, MockHttp);

            await client.GetItemsAsync<object>();

            MockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async void GetItemsTyped_DeliveryOptionsWithIncludeTotalCount_IncludeTotalCountParameterAdded()
        {
            var responseJson = JsonConvert.SerializeObject(CreateItemsResponse());
            MockHttp
                .Expect($"{BaseUrl}/items")
                .WithExactQueryString("system.type=cafe&includeTotalCount")
                .Respond("application/json", responseJson);
            var client = Factories.DeliveryClientFactory.GetMockedDeliveryClientWithOptions(Options, MockHttp);
            A.CallTo(() => client.TypeProvider.GetCodename(typeof(Cafe))).Returns("cafe");

            await client.GetItemsAsync<Cafe>();

            MockHttp.VerifyNoOutstandingExpectation();
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
}