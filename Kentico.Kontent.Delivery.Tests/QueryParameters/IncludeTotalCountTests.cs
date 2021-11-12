using System;
using FakeItEasy;
using FluentAssertions;
using Kentico.Kontent.Delivery.Abstractions;
using Kentico.Kontent.Delivery.Abstractions.Configuration;
using Kentico.Kontent.Delivery.SharedModels;
using Kentico.Kontent.Delivery.Tests.Models.ContentTypes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RichardSzalay.MockHttp;
using Xunit;

namespace Kentico.Kontent.Delivery.Tests.QueryParameters
{
    public class IncludeTotalCountTests
    {
        private readonly Guid _projectId = Guid.NewGuid();
        private  string BaseUrl => $"https://deliver.kontent.ai/{_projectId}";
        private readonly MockHttpMessageHandler _mockHttp = new MockHttpMessageHandler();
        private DeliveryOptions Options => new DeliveryOptions
        {
            ProjectId = _projectId.ToString(),
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
            _mockHttp
                .Expect($"{BaseUrl}/items")
                .WithExactQueryString("includeTotalCount")
                .Respond("application/json", responseJson);
            var client = Factories.DeliveryClientFactory.GetMockedDeliveryClientWithOptions(Options, _mockHttp);

            await client.GetItemsAsync<object>();

            _mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async void GetItemsTyped_DeliveryOptionsWithIncludeTotalCount_IncludeTotalCountParameterAdded()
        {
            var responseJson = JsonConvert.SerializeObject(CreateItemsResponse());
            _mockHttp
                .Expect($"{BaseUrl}/items")
                .WithExactQueryString("system.type=cafe&includeTotalCount")
                .Respond("application/json", responseJson);
            var client = Factories.DeliveryClientFactory.GetMockedDeliveryClientWithOptions(Options, _mockHttp);
            A.CallTo(() => client.TypeProvider.GetCodename(typeof(Cafe))).Returns("cafe");

            await client.GetItemsAsync<Cafe>();

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
}